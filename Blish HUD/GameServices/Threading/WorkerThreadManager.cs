using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Blish_HUD.GameServices.Threading {
    
    /// <summary>
    /// Categories of work that can be processed by different worker thread pools.
    /// </summary>
    public enum WorkCategory {
        /// <summary>
        /// CPU-intensive tasks like image processing, data serialization.
        /// </summary>
        CpuIntensive,
        
        /// <summary>
        /// I/O operations like file operations, network requests.
        /// </summary>
        IOOperations,
        
        /// <summary>
        /// Background processing like cache maintenance, cleanup operations.
        /// </summary>
        BackgroundProcessing,
        
        /// <summary>
        /// General purpose work that doesn't fit other categories.
        /// </summary>
        General
    }

    /// <summary>
    /// Manages multiple worker thread pools for different categories of work.
    /// Provides centralized coordination and resource management for background tasks.
    /// </summary>
    public class WorkerThreadManager : IDisposable {
        private static readonly Logger Logger = Logger.GetLogger<WorkerThreadManager>();

        private readonly ConcurrentDictionary<WorkCategory, WorkerThreadPool> _workerPools;
        private readonly ThreadSafeTaskQueue _sequentialTaskQueue;
        private volatile bool _disposed = false;

        /// <summary>
        /// Gets the singleton instance of the WorkerThreadManager.
        /// </summary>
        public static WorkerThreadManager Instance { get; private set; }

        /// <summary>
        /// Gets statistics about all worker thread pools.
        /// </summary>
        public WorkerThreadStats Statistics {
            get {
                var stats = new WorkerThreadStats();
                foreach (var pool in _workerPools.Values) {
                    stats.TotalActiveThreads += pool.ActiveThreadCount;
                    stats.TotalPendingWork += pool.PendingWorkCount;
                }
                stats.SequentialQueuePendingTasks = _sequentialTaskQueue.PendingTaskCount;
                return stats;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the WorkerThreadManager.
        /// </summary>
        /// <param name="cpuIntensiveThreads">Number of threads for CPU-intensive work. Default: ProcessorCount / 2</param>
        /// <param name="ioThreads">Number of threads for I/O operations. Default: ProcessorCount</param>
        /// <param name="backgroundThreads">Number of threads for background processing. Default: 2</param>
        /// <param name="generalThreads">Number of threads for general work. Default: ProcessorCount / 4</param>
        public static void Initialize(int cpuIntensiveThreads = 0, int ioThreads = 0, int backgroundThreads = 2, int generalThreads = 0) {
            if (Instance != null) {
                Logger.Warn("WorkerThreadManager is already initialized.");
                return;
            }

            Instance = new WorkerThreadManager(cpuIntensiveThreads, ioThreads, backgroundThreads, generalThreads);
        }

        /// <summary>
        /// Shuts down the singleton instance of the WorkerThreadManager.
        /// </summary>
        public static void Shutdown() {
            Instance?.Dispose();
            Instance = null;
        }

        private WorkerThreadManager(int cpuIntensiveThreads, int ioThreads, int backgroundThreads, int generalThreads) {
            // Calculate default thread counts based on processor count
            int processorCount = Environment.ProcessorCount;
            cpuIntensiveThreads = cpuIntensiveThreads > 0 ? cpuIntensiveThreads : Math.Max(1, processorCount / 2);
            ioThreads = ioThreads > 0 ? ioThreads : processorCount;
            backgroundThreads = backgroundThreads > 0 ? backgroundThreads : 2;
            generalThreads = generalThreads > 0 ? generalThreads : Math.Max(1, processorCount / 4);

            _workerPools = new ConcurrentDictionary<WorkCategory, WorkerThreadPool>();
            _sequentialTaskQueue = new ThreadSafeTaskQueue();

            // Initialize worker pools for each category
            _workerPools[WorkCategory.CpuIntensive] = new WorkerThreadPool(cpuIntensiveThreads);
            _workerPools[WorkCategory.IOOperations] = new WorkerThreadPool(ioThreads);
            _workerPools[WorkCategory.BackgroundProcessing] = new WorkerThreadPool(backgroundThreads);
            _workerPools[WorkCategory.General] = new WorkerThreadPool(generalThreads);

            Logger.Info($"WorkerThreadManager initialized with {cpuIntensiveThreads} CPU, {ioThreads} I/O, {backgroundThreads} background, and {generalThreads} general threads.");
        }

        /// <summary>
        /// Enqueues work to be executed by the appropriate worker thread pool.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="work">The work function to execute.</param>
        /// <param name="category">The category of work to determine which thread pool to use.</param>
        /// <param name="priority">The priority of the work item.</param>
        /// <param name="cancellationToken">Cancellation token for the work.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task<T> EnqueueWorkAsync<T>(Func<CancellationToken, Task<T>> work,
                                               WorkCategory category = WorkCategory.General,
                                               WorkPriority priority = WorkPriority.Normal,
                                               CancellationToken cancellationToken = default) {
            ThrowIfDisposed();

            if (!_workerPools.TryGetValue(category, out var pool)) {
                throw new ArgumentException($"Unknown work category: {category}", nameof(category));
            }

            return await pool.EnqueueWorkAsync(work, priority, cancellationToken);
        }

        /// <summary>
        /// Enqueues work to be executed by the appropriate worker thread pool.
        /// </summary>
        /// <param name="work">The work function to execute.</param>
        /// <param name="category">The category of work to determine which thread pool to use.</param>
        /// <param name="priority">The priority of the work item.</param>
        /// <param name="cancellationToken">Cancellation token for the work.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnqueueWorkAsync(Func<CancellationToken, Task> work,
                                         WorkCategory category = WorkCategory.General,
                                         WorkPriority priority = WorkPriority.Normal,
                                         CancellationToken cancellationToken = default) {
            ThrowIfDisposed();

            if (!_workerPools.TryGetValue(category, out var pool)) {
                throw new ArgumentException($"Unknown work category: {category}", nameof(category));
            }

            await pool.EnqueueWorkAsync(work, priority, cancellationToken);
        }

        /// <summary>
        /// Enqueues synchronous work to be executed by the appropriate worker thread pool.
        /// </summary>
        /// <param name="work">The work action to execute.</param>
        /// <param name="category">The category of work to determine which thread pool to use.</param>
        /// <param name="priority">The priority of the work item.</param>
        /// <param name="cancellationToken">Cancellation token for the work.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnqueueWorkAsync(Action<CancellationToken> work,
                                         WorkCategory category = WorkCategory.General,
                                         WorkPriority priority = WorkPriority.Normal,
                                         CancellationToken cancellationToken = default) {
            ThrowIfDisposed();

            if (!_workerPools.TryGetValue(category, out var pool)) {
                throw new ArgumentException($"Unknown work category: {category}", nameof(category));
            }

            await pool.EnqueueWorkAsync(work, priority, cancellationToken);
        }

        /// <summary>
        /// Enqueues synchronous work to be executed by the appropriate worker thread pool.
        /// </summary>
        /// <param name="work">The work action to execute.</param>
        /// <param name="category">The category of work to determine which thread pool to use.</param>
        /// <param name="priority">The priority of the work item.</param>
        /// <param name="cancellationToken">Cancellation token for the work.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnqueueWorkAsync(Action work,
                                         WorkCategory category = WorkCategory.General,
                                         WorkPriority priority = WorkPriority.Normal,
                                         CancellationToken cancellationToken = default) {
            ThrowIfDisposed();

            if (!_workerPools.TryGetValue(category, out var pool)) {
                throw new ArgumentException($"Unknown work category: {category}", nameof(category));
            }

            await pool.EnqueueWorkAsync(work, priority, cancellationToken);
        }

        /// <summary>
        /// Enqueues work to be executed sequentially in order.
        /// Use this for tasks that must be executed in a specific order.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="work">The work function to execute.</param>
        /// <param name="priority">The priority of the work item.</param>
        /// <param name="cancellationToken">Cancellation token for the work.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task<T> EnqueueSequentialWorkAsync<T>(Func<CancellationToken, Task<T>> work,
                                                          WorkPriority priority = WorkPriority.Normal,
                                                          CancellationToken cancellationToken = default) {
            ThrowIfDisposed();
            return await _sequentialTaskQueue.EnqueueAsync(work, priority, cancellationToken);
        }

        /// <summary>
        /// Enqueues work to be executed sequentially in order.
        /// Use this for tasks that must be executed in a specific order.
        /// </summary>
        /// <param name="work">The work function to execute.</param>
        /// <param name="priority">The priority of the work item.</param>
        /// <param name="cancellationToken">Cancellation token for the work.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnqueueSequentialWorkAsync(Func<CancellationToken, Task> work,
                                                    WorkPriority priority = WorkPriority.Normal,
                                                    CancellationToken cancellationToken = default) {
            ThrowIfDisposed();
            await _sequentialTaskQueue.EnqueueAsync(work, priority, cancellationToken);
        }

        /// <summary>
        /// Enqueues synchronous work to be executed sequentially in order.
        /// Use this for tasks that must be executed in a specific order.
        /// </summary>
        /// <param name="work">The work action to execute.</param>
        /// <param name="priority">The priority of the work item.</param>
        /// <param name="cancellationToken">Cancellation token for the work.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnqueueSequentialWorkAsync(Action<CancellationToken> work,
                                                    WorkPriority priority = WorkPriority.Normal,
                                                    CancellationToken cancellationToken = default) {
            ThrowIfDisposed();
            await _sequentialTaskQueue.EnqueueAsync(work, priority, cancellationToken);
        }

        /// <summary>
        /// Enqueues synchronous work to be executed sequentially in order.
        /// Use this for tasks that must be executed in a specific order.
        /// </summary>
        /// <param name="work">The work action to execute.</param>
        /// <param name="priority">The priority of the work item.</param>
        /// <param name="cancellationToken">Cancellation token for the work.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnqueueSequentialWorkAsync(Action work,
                                                    WorkPriority priority = WorkPriority.Normal,
                                                    CancellationToken cancellationToken = default) {
            ThrowIfDisposed();
            await _sequentialTaskQueue.EnqueueAsync(work, priority, cancellationToken);
        }

        private void ThrowIfDisposed() {
            if (_disposed) {
                throw new ObjectDisposedException(nameof(WorkerThreadManager));
            }
        }

        /// <summary>
        /// Disposes the worker thread manager and all associated thread pools.
        /// </summary>
        public void Dispose() {
            if (_disposed) return;

            Logger.Info("Shutting down WorkerThreadManager...");

            // Dispose all worker pools
            foreach (var pool in _workerPools.Values) {
                pool?.Dispose();
            }
            _workerPools.Clear();

            // Dispose sequential task queue
            _sequentialTaskQueue?.Dispose();

            _disposed = true;
            Logger.Info("WorkerThreadManager shutdown complete.");
        }
    }

    /// <summary>
    /// Statistics about worker thread pools.
    /// </summary>
    public class WorkerThreadStats {
        /// <summary>
        /// Total number of active worker threads across all pools.
        /// </summary>
        public int TotalActiveThreads { get; set; }

        /// <summary>
        /// Total number of pending work items across all pools.
        /// </summary>
        public int TotalPendingWork { get; set; }

        /// <summary>
        /// Number of pending tasks in the sequential task queue.
        /// </summary>
        public int SequentialQueuePendingTasks { get; set; }

        /// <summary>
        /// Returns a string representation of the statistics.
        /// </summary>
        public override string ToString() {
            return $"Active Threads: {TotalActiveThreads}, Pending Work: {TotalPendingWork}, Sequential Queue: {SequentialQueuePendingTasks}";
        }
    }
}
