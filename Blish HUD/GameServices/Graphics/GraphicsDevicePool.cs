using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Blish_HUD.Graphics {
    
    /// <summary>
    /// Priority levels for graphics device context acquisition.
    /// </summary>
    public enum ContextPriority {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Represents a pooled graphics device context with automatic resource management.
    /// </summary>
    public class PooledGraphicsDeviceContext : IDisposable {
        private readonly GraphicsDevicePool _pool;
        private readonly int _contextId;
        private volatile bool _disposed = false;

        /// <summary>
        /// Gets the GraphicsDevice associated with this context.
        /// </summary>
        public GraphicsDevice GraphicsDevice { get; }

        /// <summary>
        /// Gets the unique identifier for this context.
        /// </summary>
        public int ContextId => _contextId;

        /// <summary>
        /// Gets a value indicating whether this context has been disposed.
        /// </summary>
        public bool IsDisposed => _disposed;

        internal PooledGraphicsDeviceContext(GraphicsDevicePool pool, GraphicsDevice graphicsDevice, int contextId) {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            GraphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            _contextId = contextId;
        }

        /// <summary>
        /// Returns the context to the pool for reuse.
        /// </summary>
        public void Dispose() {
            if (_disposed) return;
            
            _disposed = true;
            _pool.ReturnContext(this);
        }
    }

    /// <summary>
    /// A high-performance graphics device context pool that reduces lock contention
    /// and provides priority-based context allocation.
    /// </summary>
    public class GraphicsDevicePool : IDisposable {
        private static readonly Logger Logger = Logger.GetLogger<GraphicsDevicePool>();

        private readonly GraphicsDevice _graphicsDevice;
        private readonly ConcurrentQueue<PooledGraphicsDeviceContext> _availableContexts;
        private readonly SemaphoreSlim _contextSemaphore;
        private readonly ConcurrentQueue<ContextRequest>[] _priorityQueues;
        private readonly CancellationTokenSource _shutdownToken;
        private readonly Task _processingTask;
        private readonly object _poolLock = new object();
        
        private volatile int _nextContextId = 1;
        private volatile int _maxContexts;
        private volatile int _currentContextCount = 0;
        private volatile bool _disposed = false;

        /// <summary>
        /// Gets the maximum number of contexts that can be created.
        /// </summary>
        public int MaxContexts => _maxContexts;

        /// <summary>
        /// Gets the current number of active contexts.
        /// </summary>
        public int ActiveContextCount => _currentContextCount;

        /// <summary>
        /// Gets the number of available contexts in the pool.
        /// </summary>
        public int AvailableContextCount => _availableContexts.Count;

        /// <summary>
        /// Gets the total number of pending context requests across all priority levels.
        /// </summary>
        public int PendingRequestCount {
            get {
                int total = 0;
                for (int i = 0; i < _priorityQueues.Length; i++) {
                    total += _priorityQueues[i].Count;
                }
                return total;
            }
        }

        /// <summary>
        /// Initializes a new instance of the GraphicsDevicePool.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device to create contexts for.</param>
        /// <param name="maxContexts">Maximum number of contexts to maintain. Default: 8</param>
        public GraphicsDevicePool(GraphicsDevice graphicsDevice, int maxContexts = 8) {
            _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            _maxContexts = Math.Max(1, maxContexts);

            _availableContexts = new ConcurrentQueue<PooledGraphicsDeviceContext>();
            _contextSemaphore = new SemaphoreSlim(_maxContexts, _maxContexts);
            _shutdownToken = new CancellationTokenSource();

            // Create priority queues for each priority level
            _priorityQueues = new ConcurrentQueue<ContextRequest>[Enum.GetValues(typeof(ContextPriority)).Length];
            for (int i = 0; i < _priorityQueues.Length; i++) {
                _priorityQueues[i] = new ConcurrentQueue<ContextRequest>();
            }

            // Start the request processing task
            _processingTask = Task.Run(ProcessRequestsAsync, _shutdownToken.Token);

            Logger.Debug($"GraphicsDevicePool initialized with max {_maxContexts} contexts.");
        }

        /// <summary>
        /// Acquires a graphics device context asynchronously with the specified priority and timeout.
        /// </summary>
        /// <param name="priority">The priority of the context request.</param>
        /// <param name="timeout">The maximum time to wait for a context. Default: 5 seconds</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>A pooled graphics device context.</returns>
        public async Task<PooledGraphicsDeviceContext> AcquireContextAsync(
            ContextPriority priority = ContextPriority.Normal,
            TimeSpan timeout = default,
            CancellationToken cancellationToken = default) {
            
            ThrowIfDisposed();

            var effectiveTimeout = timeout == default ? TimeSpan.FromSeconds(5) : timeout;
            
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(effectiveTimeout);

            var request = new ContextRequest(priority, cts.Token);
            
            // Enqueue the request based on priority
            var priorityIndex = (int)priority;
            _priorityQueues[priorityIndex].Enqueue(request);

            try {
                return await request.CompletionSource.Task;
            } catch (OperationCanceledException) when (cts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested) {
                throw new TimeoutException($"Failed to acquire graphics device context within {effectiveTimeout.TotalMilliseconds}ms");
            }
        }

        /// <summary>
        /// Tries to acquire a graphics device context synchronously.
        /// This method should only be used when async acquisition is not possible.
        /// </summary>
        /// <param name="context">The acquired context, if successful.</param>
        /// <param name="priority">The priority of the context request.</param>
        /// <returns>True if a context was acquired; otherwise, false.</returns>
        public bool TryAcquireContext(out PooledGraphicsDeviceContext context, ContextPriority priority = ContextPriority.Normal) {
            context = null;
            
            if (_disposed) return false;

            // Try to get an available context immediately
            if (_availableContexts.TryDequeue(out context)) {
                return true;
            }

            // Try to create a new context if under the limit
            lock (_poolLock) {
                if (_currentContextCount < _maxContexts) {
                    context = CreateNewContext();
                    return context != null;
                }
            }

            return false;
        }

        internal void ReturnContext(PooledGraphicsDeviceContext context) {
            if (_disposed || context == null) return;

            // Reset context state if needed
            try {
                // Add any context cleanup logic here
                _availableContexts.Enqueue(context);
                _contextSemaphore.Release();
            } catch (Exception ex) {
                Logger.Warn(ex, $"Error returning graphics device context {context.ContextId} to pool.");
            }
        }

        private async Task ProcessRequestsAsync() {
            try {
                while (!_shutdownToken.Token.IsCancellationRequested) {
                    try {
                        // Wait for context availability
                        await _contextSemaphore.WaitAsync(_shutdownToken.Token);

                        // Process requests in priority order
                        ContextRequest request = null;
                        for (int priority = _priorityQueues.Length - 1; priority >= 0; priority--) {
                            if (_priorityQueues[priority].TryDequeue(out request)) {
                                break;
                            }
                        }

                        if (request == null) {
                            // No requests found, release the semaphore
                            _contextSemaphore.Release();
                            await Task.Delay(1, _shutdownToken.Token);
                            continue;
                        }

                        if (request.CancellationToken.IsCancellationRequested) {
                            request.CompletionSource.SetCanceled();
                            _contextSemaphore.Release();
                            continue;
                        }

                        // Try to get an available context or create a new one
                        PooledGraphicsDeviceContext context = null;
                        
                        if (!_availableContexts.TryDequeue(out context)) {
                            context = CreateNewContext();
                        }

                        if (context != null) {
                            request.CompletionSource.SetResult(context);
                        } else {
                            request.CompletionSource.SetException(new InvalidOperationException("Failed to create graphics device context"));
                            _contextSemaphore.Release();
                        }
                    } catch (OperationCanceledException) {
                        // Expected during shutdown
                        break;
                    } catch (Exception ex) {
                        Logger.Warn(ex, "Unhandled exception in graphics device pool processing.");
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex, "Fatal error in graphics device pool processing loop.");
            }
        }

        private PooledGraphicsDeviceContext CreateNewContext() {
            lock (_poolLock) {
                if (_currentContextCount >= _maxContexts) {
                    return null;
                }

                try {
                    var contextId = Interlocked.Increment(ref _nextContextId);
                    var context = new PooledGraphicsDeviceContext(this, _graphicsDevice, contextId);
                    
                    Interlocked.Increment(ref _currentContextCount);
                    
                    Logger.Debug($"Created new graphics device context {contextId}. Active contexts: {_currentContextCount}");
                    return context;
                } catch (Exception ex) {
                    Logger.Error(ex, "Failed to create new graphics device context.");
                    return null;
                }
            }
        }

        private void ThrowIfDisposed() {
            if (_disposed) {
                throw new ObjectDisposedException(nameof(GraphicsDevicePool));
            }
        }

        /// <summary>
        /// Disposes the graphics device pool and all associated resources.
        /// </summary>
        public void Dispose() {
            if (_disposed) return;

            Logger.Debug("Shutting down GraphicsDevicePool...");

            _shutdownToken.Cancel();

            try {
                // Wait for processing task to complete
                _processingTask?.Wait(TimeSpan.FromSeconds(5));
            } catch (AggregateException ex) {
                Logger.Debug($"Graphics device pool shutdown completed with expected cancellation: {ex.InnerException?.Message}");
            }

            // Cancel any pending requests
            for (int priority = 0; priority < _priorityQueues.Length; priority++) {
                while (_priorityQueues[priority].TryDequeue(out var request)) {
                    request.CompletionSource.SetCanceled();
                }
            }

            // Clear available contexts
            while (_availableContexts.TryDequeue(out var context)) {
                // Contexts will be disposed when their references are released
            }

            _contextSemaphore?.Dispose();
            _shutdownToken?.Dispose();

            _disposed = true;
            Logger.Debug("GraphicsDevicePool shutdown complete.");
        }

        /// <summary>
        /// Represents a context acquisition request.
        /// </summary>
        private class ContextRequest {
            public ContextPriority Priority { get; }
            public CancellationToken CancellationToken { get; }
            public TaskCompletionSource<PooledGraphicsDeviceContext> CompletionSource { get; }

            public ContextRequest(ContextPriority priority, CancellationToken cancellationToken) {
                Priority = priority;
                CancellationToken = cancellationToken;
                CompletionSource = new TaskCompletionSource<PooledGraphicsDeviceContext>();
            }
        }
    }
}
