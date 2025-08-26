using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Blish_HUD.GameServices.Threading {
    
    /// <summary>
    /// A thread-safe task queue that supports priority-based task execution.
    /// Provides ordered execution of tasks with cancellation support.
    /// </summary>
    public class ThreadSafeTaskQueue : IDisposable {
        private static readonly Logger Logger = Logger.GetLogger<ThreadSafeTaskQueue>();

        private readonly ConcurrentQueue<QueuedTaskBase> _taskQueue;
        private readonly SemaphoreSlim _taskAvailable;
        private readonly CancellationTokenSource _shutdownToken;
        private readonly Task _processingTask;
        private volatile bool _disposed = false;

        /// <summary>
        /// Gets the number of pending tasks in the queue.
        /// </summary>
        public int PendingTaskCount => _taskQueue.Count;

        /// <summary>
        /// Gets a value indicating whether the queue is currently processing tasks.
        /// </summary>
        public bool IsProcessing => !_processingTask.IsCompleted;

        /// <summary>
        /// Initializes a new instance of the ThreadSafeTaskQueue.
        /// </summary>
        public ThreadSafeTaskQueue() {
            _taskQueue = new ConcurrentQueue<QueuedTaskBase>();
            _taskAvailable = new SemaphoreSlim(0);
            _shutdownToken = new CancellationTokenSource();
            
            // Start the processing task
            _processingTask = Task.Run(ProcessTasksAsync, _shutdownToken.Token);
            
            Logger.Debug("ThreadSafeTaskQueue initialized.");
        }

        /// <summary>
        /// Enqueues a task for execution.
        /// </summary>
        /// <param name="taskFactory">Factory function that creates the task to execute.</param>
        /// <param name="priority">Priority of the task.</param>
        /// <param name="cancellationToken">Cancellation token for the task.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task EnqueueAsync(Func<CancellationToken, Task> taskFactory, 
                                WorkPriority priority = WorkPriority.Normal,
                                CancellationToken cancellationToken = default) {
            ThrowIfDisposed();

            var queuedTask = new QueuedTask(taskFactory, priority, cancellationToken);
            _taskQueue.Enqueue(queuedTask);
            _taskAvailable.Release();
            
            return queuedTask.CompletionSource.Task;
        }

        /// <summary>
        /// Enqueues a task for execution with a result.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="taskFactory">Factory function that creates the task to execute.</param>
        /// <param name="priority">Priority of the task.</param>
        /// <param name="cancellationToken">Cancellation token for the task.</param>
        /// <returns>A task representing the asynchronous operation with result.</returns>
        public Task<T> EnqueueAsync<T>(Func<CancellationToken, Task<T>> taskFactory, 
                                      WorkPriority priority = WorkPriority.Normal,
                                      CancellationToken cancellationToken = default) {
            ThrowIfDisposed();

            var queuedTask = new QueuedTask<T>(taskFactory, priority, cancellationToken);
            _taskQueue.Enqueue(queuedTask);
            _taskAvailable.Release();
            
            return queuedTask.CompletionSource.Task;
        }

        /// <summary>
        /// Enqueues a synchronous action for execution.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="priority">Priority of the task.</param>
        /// <param name="cancellationToken">Cancellation token for the task.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task EnqueueAsync(Action<CancellationToken> action, 
                                WorkPriority priority = WorkPriority.Normal,
                                CancellationToken cancellationToken = default) {
            return EnqueueAsync(ct => {
                action(ct);
                return Task.CompletedTask;
            }, priority, cancellationToken);
        }

        /// <summary>
        /// Enqueues a synchronous action for execution.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="priority">Priority of the task.</param>
        /// <param name="cancellationToken">Cancellation token for the task.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task EnqueueAsync(Action action, 
                                WorkPriority priority = WorkPriority.Normal,
                                CancellationToken cancellationToken = default) {
            return EnqueueAsync(_ => action(), priority, cancellationToken);
        }

        private async Task ProcessTasksAsync() {
            try {
                while (!_shutdownToken.Token.IsCancellationRequested) {
                    try {
                        // Wait for tasks to become available
                        await _taskAvailable.WaitAsync(_shutdownToken.Token);

                        // Process all available tasks in priority order
                        var availableTasks = new List<QueuedTaskBase>();
                        
                        // Collect all available tasks
                        while (_taskQueue.TryDequeue(out var task)) {
                            availableTasks.Add(task);
                        }

                        if (availableTasks.Count == 0) continue;

                        // Sort by priority (highest first)
                        availableTasks.Sort((a, b) => b.Priority.CompareTo(a.Priority));

                        // Execute tasks in priority order
                        foreach (var task in availableTasks) {
                            if (_shutdownToken.Token.IsCancellationRequested) break;
                            
                            await task.ExecuteAsync();
                        }
                    } catch (OperationCanceledException) {
                        // Expected during shutdown
                        break;
                    } catch (Exception ex) {
                        Logger.Warn(ex, "Unhandled exception in task queue processing.");
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex, "Fatal error in task queue processing loop.");
            }
        }

        private void ThrowIfDisposed() {
            if (_disposed) {
                throw new ObjectDisposedException(nameof(ThreadSafeTaskQueue));
            }
        }

        /// <summary>
        /// Disposes the task queue and waits for processing to complete.
        /// </summary>
        public void Dispose() {
            if (_disposed) return;

            Logger.Debug("Shutting down ThreadSafeTaskQueue...");

            _shutdownToken.Cancel();

            try {
                // Wait for processing task to complete
                _processingTask?.Wait(TimeSpan.FromSeconds(5));
            } catch (AggregateException ex) {
                // Expected if task was cancelled
                Logger.Debug($"Task queue shutdown completed with expected cancellation: {ex.InnerException?.Message}");
            }

            _taskAvailable?.Dispose();
            _shutdownToken?.Dispose();

            _disposed = true;
            Logger.Debug("ThreadSafeTaskQueue shutdown complete.");
        }

        /// <summary>
        /// Base class for queued tasks.
        /// </summary>
        private abstract class QueuedTaskBase {
            public WorkPriority Priority { get; }
            public CancellationToken CancellationToken { get; }

            protected QueuedTaskBase(WorkPriority priority, CancellationToken cancellationToken) {
                Priority = priority;
                CancellationToken = cancellationToken;
            }

            public abstract Task ExecuteAsync();
        }

        /// <summary>
        /// Represents a queued task without a result.
        /// </summary>
        private class QueuedTask : QueuedTaskBase {
            private readonly Func<CancellationToken, Task> _taskFactory;
            
            public TaskCompletionSource<bool> CompletionSource { get; }

            public QueuedTask(Func<CancellationToken, Task> taskFactory, WorkPriority priority, CancellationToken cancellationToken) 
                : base(priority, cancellationToken) {
                _taskFactory = taskFactory ?? throw new ArgumentNullException(nameof(taskFactory));
                CompletionSource = new TaskCompletionSource<bool>();
            }

            public override async Task ExecuteAsync() {
                try {
                    if (CancellationToken.IsCancellationRequested) {
                        CompletionSource.SetCanceled();
                        return;
                    }

                    await _taskFactory(CancellationToken);
                    CompletionSource.SetResult(true);
                } catch (OperationCanceledException) {
                    CompletionSource.SetCanceled();
                } catch (Exception ex) {
                    CompletionSource.SetException(ex);
                }
            }
        }

        /// <summary>
        /// Represents a queued task with a result.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        private class QueuedTask<T> : QueuedTaskBase {
            private readonly Func<CancellationToken, Task<T>> _taskFactory;
            
            public TaskCompletionSource<T> CompletionSource { get; }

            public QueuedTask(Func<CancellationToken, Task<T>> taskFactory, WorkPriority priority, CancellationToken cancellationToken) 
                : base(priority, cancellationToken) {
                _taskFactory = taskFactory ?? throw new ArgumentNullException(nameof(taskFactory));
                CompletionSource = new TaskCompletionSource<T>();
            }

            public override async Task ExecuteAsync() {
                try {
                    if (CancellationToken.IsCancellationRequested) {
                        CompletionSource.SetCanceled();
                        return;
                    }

                    var result = await _taskFactory(CancellationToken);
                    CompletionSource.SetResult(result);
                } catch (OperationCanceledException) {
                    CompletionSource.SetCanceled();
                } catch (Exception ex) {
                    CompletionSource.SetException(ex);
                }
            }
        }
    }
}
