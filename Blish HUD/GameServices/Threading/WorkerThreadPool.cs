using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Blish_HUD.GameServices.Threading {
    
    /// <summary>
    /// Priority levels for work items in the worker thread pool.
    /// </summary>
    public enum WorkPriority {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Interface for work items that can be executed by the worker thread pool.
    /// </summary>
    internal interface IWorkItem {
        WorkPriority Priority { get; }
        CancellationToken CancellationToken { get; }
        Task ExecuteAsync();
    }

    /// <summary>
    /// Represents an asynchronous work item with a result.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    internal class AsyncWorkItem<T> : IWorkItem {
        private readonly Func<CancellationToken, Task<T>> _work;
        
        public WorkPriority Priority { get; }
        public CancellationToken CancellationToken { get; }
        public TaskCompletionSource<T> CompletionSource { get; }

        public AsyncWorkItem(Func<CancellationToken, Task<T>> work, WorkPriority priority, CancellationToken cancellationToken) {
            _work = work ?? throw new ArgumentNullException(nameof(work));
            Priority = priority;
            CancellationToken = cancellationToken;
            CompletionSource = new TaskCompletionSource<T>();
        }

        public async Task ExecuteAsync() {
            try {
                if (CancellationToken.IsCancellationRequested) {
                    CompletionSource.SetCanceled();
                    return;
                }

                var result = await _work(CancellationToken);
                CompletionSource.SetResult(result);
            } catch (OperationCanceledException) {
                CompletionSource.SetCanceled();
            } catch (Exception ex) {
                CompletionSource.SetException(ex);
            }
        }
    }

    /// <summary>
    /// Represents a synchronous work item without a result.
    /// </summary>
    internal class SyncWorkItem : IWorkItem {
        private readonly Action<CancellationToken> _work;
        
        public WorkPriority Priority { get; }
        public CancellationToken CancellationToken { get; }
        public TaskCompletionSource<bool> CompletionSource { get; }

        public SyncWorkItem(Action<CancellationToken> work, WorkPriority priority, CancellationToken cancellationToken) {
            _work = work ?? throw new ArgumentNullException(nameof(work));
            Priority = priority;
            CancellationToken = cancellationToken;
            CompletionSource = new TaskCompletionSource<bool>();
        }

        public Task ExecuteAsync() {
            try {
                if (CancellationToken.IsCancellationRequested) {
                    CompletionSource.SetCanceled();
                    return Task.CompletedTask;
                }

                _work(CancellationToken);
                CompletionSource.SetResult(true);
            } catch (OperationCanceledException) {
                CompletionSource.SetCanceled();
            } catch (Exception ex) {
                CompletionSource.SetException(ex);
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// A dedicated worker thread pool for executing background tasks with priority support.
    /// Designed to move CPU-intensive and I/O operations off the UI thread.
    /// </summary>
    public class WorkerThreadPool : IDisposable {
        private static readonly Logger Logger = Logger.GetLogger<WorkerThreadPool>();

        private readonly ConcurrentQueue<IWorkItem>[] _priorityQueues;
        private readonly SemaphoreSlim _workAvailable;
        private readonly CancellationTokenSource _shutdownToken;
        private readonly Thread[] _workerThreads;
        private readonly int _maxThreads;
        private volatile bool _disposed = false;

        /// <summary>
        /// Gets the number of active worker threads.
        /// </summary>
        public int ActiveThreadCount => _workerThreads?.Length ?? 0;

        /// <summary>
        /// Gets the total number of pending work items across all priority queues.
        /// </summary>
        public int PendingWorkCount {
            get {
                int total = 0;
                for (int i = 0; i < _priorityQueues.Length; i++) {
                    total += _priorityQueues[i].Count;
                }
                return total;
            }
        }

        /// <summary>
        /// Initializes a new instance of the WorkerThreadPool.
        /// </summary>
        /// <param name="maxThreads">Maximum number of worker threads. If not specified, uses processor count.</param>
        public WorkerThreadPool(int maxThreads = 0) {
            _maxThreads = maxThreads > 0 ? maxThreads : Environment.ProcessorCount;
            
            // Create priority queues for each priority level
            _priorityQueues = new ConcurrentQueue<IWorkItem>[Enum.GetValues(typeof(WorkPriority)).Length];
            for (int i = 0; i < _priorityQueues.Length; i++) {
                _priorityQueues[i] = new ConcurrentQueue<IWorkItem>();
            }

            _workAvailable = new SemaphoreSlim(0);
            _shutdownToken = new CancellationTokenSource();
            _workerThreads = new Thread[_maxThreads];

            // Start worker threads
            for (int i = 0; i < _maxThreads; i++) {
                _workerThreads[i] = new Thread(WorkerThreadLoop) {
                    Name = $"BlishHUD-Worker-{i}",
                    IsBackground = true
                };
                _workerThreads[i].Start();
            }

            Logger.Info($"Worker thread pool initialized with {_maxThreads} threads.");
        }

        /// <summary>
        /// Enqueues asynchronous work to be executed by the worker thread pool.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="work">The work function to execute.</param>
        /// <param name="priority">The priority of the work item.</param>
        /// <param name="cancellationToken">Cancellation token for the work.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task<T> EnqueueWorkAsync<T>(Func<CancellationToken, Task<T>> work, 
                                               WorkPriority priority = WorkPriority.Normal,
                                               CancellationToken cancellationToken = default) {
            ThrowIfDisposed();

            var workItem = new AsyncWorkItem<T>(work, priority, cancellationToken);
            EnqueueWorkItem(workItem);
            
            return await workItem.CompletionSource.Task;
        }

        /// <summary>
        /// Enqueues asynchronous work to be executed by the worker thread pool.
        /// </summary>
        /// <param name="work">The work function to execute.</param>
        /// <param name="priority">The priority of the work item.</param>
        /// <param name="cancellationToken">Cancellation token for the work.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnqueueWorkAsync(Func<CancellationToken, Task> work, 
                                         WorkPriority priority = WorkPriority.Normal,
                                         CancellationToken cancellationToken = default) {
            await EnqueueWorkAsync(async ct => {
                await work(ct);
                return true;
            }, priority, cancellationToken);
        }

        /// <summary>
        /// Enqueues synchronous work to be executed by the worker thread pool.
        /// </summary>
        /// <param name="work">The work action to execute.</param>
        /// <param name="priority">The priority of the work item.</param>
        /// <param name="cancellationToken">Cancellation token for the work.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnqueueWorkAsync(Action<CancellationToken> work, 
                                         WorkPriority priority = WorkPriority.Normal,
                                         CancellationToken cancellationToken = default) {
            ThrowIfDisposed();

            var workItem = new SyncWorkItem(work, priority, cancellationToken);
            EnqueueWorkItem(workItem);
            
            await workItem.CompletionSource.Task;
        }

        /// <summary>
        /// Enqueues synchronous work to be executed by the worker thread pool.
        /// </summary>
        /// <param name="work">The work action to execute.</param>
        /// <param name="priority">The priority of the work item.</param>
        /// <param name="cancellationToken">Cancellation token for the work.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnqueueWorkAsync(Action work, 
                                         WorkPriority priority = WorkPriority.Normal,
                                         CancellationToken cancellationToken = default) {
            await EnqueueWorkAsync(_ => work(), priority, cancellationToken);
        }

        private void EnqueueWorkItem(IWorkItem workItem) {
            var priorityIndex = (int)workItem.Priority;
            _priorityQueues[priorityIndex].Enqueue(workItem);
            _workAvailable.Release();
        }

        private async void WorkerThreadLoop() {
            try {
                while (!_shutdownToken.Token.IsCancellationRequested) {
                    try {
                        // Wait for work to become available
                        await _workAvailable.WaitAsync(_shutdownToken.Token);

                        // Try to get work item with highest priority first
                        IWorkItem workItem = null;
                        for (int priority = _priorityQueues.Length - 1; priority >= 0; priority--) {
                            if (_priorityQueues[priority].TryDequeue(out workItem)) {
                                break;
                            }
                        }

                        if (workItem != null) {
                            await workItem.ExecuteAsync();
                        }
                    } catch (OperationCanceledException) {
                        // Expected during shutdown
                        break;
                    } catch (Exception ex) {
                        Logger.Warn(ex, "Unhandled exception in worker thread.");
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex, "Fatal error in worker thread loop.");
            }
        }

        private void ThrowIfDisposed() {
            if (_disposed) {
                throw new ObjectDisposedException(nameof(WorkerThreadPool));
            }
        }

        /// <summary>
        /// Disposes the worker thread pool and waits for all threads to complete.
        /// </summary>
        public void Dispose() {
            if (_disposed) return;

            Logger.Info("Shutting down worker thread pool...");

            _shutdownToken.Cancel();

            // Wait for all worker threads to complete
            if (_workerThreads != null) {
                foreach (var thread in _workerThreads) {
                    if (thread?.IsAlive == true) {
                        thread.Join(TimeSpan.FromSeconds(5));
                    }
                }
            }

            _workAvailable?.Dispose();
            _shutdownToken?.Dispose();

            _disposed = true;
            Logger.Info("Worker thread pool shutdown complete.");
        }
    }
}
