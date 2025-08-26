using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;

namespace Blish_HUD.Graphics {
    
    /// <summary>
    /// Defines priority levels for render commands to ensure critical rendering operations
    /// are processed before less important ones.
    /// </summary>
    public enum RenderCommandPriority {
        /// <summary>
        /// Background operations that can be delayed without affecting user experience.
        /// Examples: texture loading, cache updates, non-visible element rendering.
        /// </summary>
        Background = 0,
        
        /// <summary>
        /// Standard priority for most UI rendering operations.
        /// Examples: regular UI controls, panels, standard text rendering.
        /// </summary>
        Normal = 1,
        
        /// <summary>
        /// High priority for important UI elements that should render quickly.
        /// Examples: tooltips, context menus, active window content.
        /// </summary>
        High = 2,
        
        /// <summary>
        /// Critical priority for essential rendering operations that must complete immediately.
        /// Examples: screen clearing, essential UI updates, error dialogs.
        /// </summary>
        Critical = 3
    }

    /// <summary>
    /// Represents a prioritized render command with metadata for efficient processing.
    /// </summary>
    public class PrioritizedRenderCommand {
        private static readonly Logger Logger = Logger.GetLogger<PrioritizedRenderCommand>();
        
        /// <summary>
        /// Gets the unique identifier for this command.
        /// </summary>
        public int CommandId { get; }
        
        /// <summary>
        /// Gets the priority level of this command.
        /// </summary>
        public RenderCommandPriority Priority { get; }
        
        /// <summary>
        /// Gets the render command to execute.
        /// </summary>
        public Action<GraphicsDevice> Command { get; }
        
        /// <summary>
        /// Gets the time when this command was created.
        /// </summary>
        public DateTime CreatedAt { get; }
        
        /// <summary>
        /// Gets the estimated execution time for this command in milliseconds.
        /// </summary>
        public float EstimatedExecutionTimeMs { get; }
        
        /// <summary>
        /// Gets optional metadata associated with this command.
        /// </summary>
        public string Metadata { get; }
        
        /// <summary>
        /// Gets a value indicating whether this command can be batched with others.
        /// </summary>
        public bool CanBatch { get; }

        /// <summary>
        /// Initializes a new instance of the PrioritizedRenderCommand class.
        /// </summary>
        /// <param name="commandId">The unique identifier for this command.</param>
        /// <param name="priority">The priority level of this command.</param>
        /// <param name="command">The render command to execute.</param>
        /// <param name="estimatedExecutionTimeMs">The estimated execution time in milliseconds.</param>
        /// <param name="metadata">Optional metadata for debugging and profiling.</param>
        /// <param name="canBatch">Whether this command can be batched with others.</param>
        public PrioritizedRenderCommand(
            int commandId,
            RenderCommandPriority priority,
            Action<GraphicsDevice> command,
            float estimatedExecutionTimeMs = 1.0f,
            string metadata = null,
            bool canBatch = true) {
            
            CommandId = commandId;
            Priority = priority;
            Command = command ?? throw new ArgumentNullException(nameof(command));
            EstimatedExecutionTimeMs = Math.Max(0.1f, estimatedExecutionTimeMs);
            Metadata = metadata ?? string.Empty;
            CanBatch = canBatch;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Executes this render command and returns execution statistics.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device to use for rendering.</param>
        /// <returns>Execution statistics for this command.</returns>
        public CommandExecutionStats Execute(GraphicsDevice graphicsDevice) {
            if (graphicsDevice == null) {
                throw new ArgumentNullException(nameof(graphicsDevice));
            }

            var stopwatch = Stopwatch.StartNew();
            bool success = false;
            Exception exception = null;

            try {
                Command.Invoke(graphicsDevice);
                success = true;
            } catch (Exception ex) {
                exception = ex;
                Logger.Warn(ex, $"Error executing render command {CommandId} (Priority: {Priority}).");
            } finally {
                stopwatch.Stop();
            }

            return new CommandExecutionStats(
                CommandId,
                Priority,
                stopwatch.ElapsedMilliseconds,
                EstimatedExecutionTimeMs,
                success,
                exception,
                Metadata
            );
        }

        /// <summary>
        /// Gets a string representation of this command for debugging.
        /// </summary>
        public override string ToString() {
            var age = DateTime.UtcNow - CreatedAt;
            return $"Command {CommandId} [{Priority}] - Est: {EstimatedExecutionTimeMs:F1}ms, Age: {age.TotalMilliseconds:F1}ms, Batch: {CanBatch}";
        }
    }

    /// <summary>
    /// Contains execution statistics for a render command.
    /// </summary>
    public class CommandExecutionStats {
        /// <summary>
        /// Gets the command ID that was executed.
        /// </summary>
        public int CommandId { get; }
        
        /// <summary>
        /// Gets the priority of the executed command.
        /// </summary>
        public RenderCommandPriority Priority { get; }
        
        /// <summary>
        /// Gets the actual execution time in milliseconds.
        /// </summary>
        public long ActualExecutionTimeMs { get; }
        
        /// <summary>
        /// Gets the estimated execution time in milliseconds.
        /// </summary>
        public float EstimatedExecutionTimeMs { get; }
        
        /// <summary>
        /// Gets a value indicating whether the command executed successfully.
        /// </summary>
        public bool Success { get; }
        
        /// <summary>
        /// Gets the exception that occurred during execution, if any.
        /// </summary>
        public Exception Exception { get; }
        
        /// <summary>
        /// Gets the metadata associated with the command.
        /// </summary>
        public string Metadata { get; }
        
        /// <summary>
        /// Gets the accuracy of the time estimation (1.0 = perfect, >1.0 = underestimated, <1.0 = overestimated).
        /// </summary>
        public float EstimationAccuracy => EstimatedExecutionTimeMs > 0 
            ? ActualExecutionTimeMs / EstimatedExecutionTimeMs 
            : 1.0f;

        /// <summary>
        /// Initializes a new instance of the CommandExecutionStats class.
        /// </summary>
        public CommandExecutionStats(
            int commandId,
            RenderCommandPriority priority,
            long actualExecutionTimeMs,
            float estimatedExecutionTimeMs,
            bool success,
            Exception exception,
            string metadata) {
            
            CommandId = commandId;
            Priority = priority;
            ActualExecutionTimeMs = actualExecutionTimeMs;
            EstimatedExecutionTimeMs = estimatedExecutionTimeMs;
            Success = success;
            Exception = exception;
            Metadata = metadata ?? string.Empty;
        }

        /// <summary>
        /// Gets a string representation of these execution statistics.
        /// </summary>
        public override string ToString() {
            var status = Success ? "✓" : "✗";
            return $"{status} Command {CommandId} [{Priority}]: {ActualExecutionTimeMs}ms (est: {EstimatedExecutionTimeMs:F1}ms, accuracy: {EstimationAccuracy:F2}x)";
        }
    }

    /// <summary>
    /// Manages priority-based sorting and processing of render commands.
    /// </summary>
    public class RenderCommandPriorityManager {
        private static readonly Logger Logger = Logger.GetLogger<RenderCommandPriorityManager>();

        private readonly Dictionary<RenderCommandPriority, Queue<PrioritizedRenderCommand>> _priorityQueues;
        private readonly object _queueLock = new object();
        private int _nextCommandId = 1;

        // Performance tracking
        private readonly Dictionary<RenderCommandPriority, long> _totalExecutionTimes;
        private readonly Dictionary<RenderCommandPriority, int> _executionCounts;

        /// <summary>
        /// Gets the total number of queued commands across all priorities.
        /// </summary>
        public int TotalQueuedCommands {
            get {
                lock (_queueLock) {
                    int total = 0;
                    foreach (var queue in _priorityQueues.Values) {
                        total += queue.Count;
                    }
                    return total;
                }
            }
        }

        /// <summary>
        /// Gets the number of queued commands for a specific priority level.
        /// </summary>
        /// <param name="priority">The priority level to check.</param>
        /// <returns>The number of queued commands at the specified priority.</returns>
        public int GetQueuedCommandCount(RenderCommandPriority priority) {
            lock (_queueLock) {
                return _priorityQueues.TryGetValue(priority, out var queue) ? queue.Count : 0;
            }
        }

        /// <summary>
        /// Initializes a new instance of the RenderCommandPriorityManager class.
        /// </summary>
        public RenderCommandPriorityManager() {
            _priorityQueues = new Dictionary<RenderCommandPriority, Queue<PrioritizedRenderCommand>>();
            _totalExecutionTimes = new Dictionary<RenderCommandPriority, long>();
            _executionCounts = new Dictionary<RenderCommandPriority, int>();

            // Initialize queues for each priority level
            foreach (RenderCommandPriority priority in Enum.GetValues(typeof(RenderCommandPriority))) {
                _priorityQueues[priority] = new Queue<PrioritizedRenderCommand>();
                _totalExecutionTimes[priority] = 0;
                _executionCounts[priority] = 0;
            }

            Logger.Debug("RenderCommandPriorityManager initialized with priority queues for all priority levels.");
        }

        /// <summary>
        /// Enqueues a render command with the specified priority.
        /// </summary>
        /// <param name="priority">The priority level for the command.</param>
        /// <param name="command">The render command to execute.</param>
        /// <param name="estimatedExecutionTimeMs">The estimated execution time in milliseconds.</param>
        /// <param name="metadata">Optional metadata for debugging and profiling.</param>
        /// <param name="canBatch">Whether this command can be batched with others.</param>
        /// <returns>The unique command ID assigned to this command.</returns>
        public int EnqueueCommand(
            RenderCommandPriority priority,
            Action<GraphicsDevice> command,
            float estimatedExecutionTimeMs = 1.0f,
            string metadata = null,
            bool canBatch = true) {
            
            if (command == null) {
                throw new ArgumentNullException(nameof(command));
            }

            var commandId = System.Threading.Interlocked.Increment(ref _nextCommandId);
            var prioritizedCommand = new PrioritizedRenderCommand(
                commandId, priority, command, estimatedExecutionTimeMs, metadata, canBatch);

            lock (_queueLock) {
                _priorityQueues[priority].Enqueue(prioritizedCommand);
            }

            Logger.Debug($"Enqueued command {commandId} with priority {priority}. Queue size: {GetQueuedCommandCount(priority)}");
            return commandId;
        }

        /// <summary>
        /// Dequeues the highest priority command available.
        /// </summary>
        /// <returns>The highest priority command, or null if no commands are queued.</returns>
        public PrioritizedRenderCommand DequeueHighestPriorityCommand() {
            lock (_queueLock) {
                // Process priorities from highest to lowest
                foreach (RenderCommandPriority priority in Enum.GetValues(typeof(RenderCommandPriority)).Cast<RenderCommandPriority>().OrderByDescending(p => p)) {
                    if (_priorityQueues[priority].Count > 0) {
                        return _priorityQueues[priority].Dequeue();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Dequeues multiple commands up to the specified limit, prioritizing higher priority commands.
        /// </summary>
        /// <param name="maxCommands">The maximum number of commands to dequeue.</param>
        /// <param name="maxEstimatedTimeMs">The maximum total estimated execution time for all commands.</param>
        /// <returns>A list of prioritized render commands.</returns>
        public List<PrioritizedRenderCommand> DequeueCommands(int maxCommands, float maxEstimatedTimeMs = float.MaxValue) {
            var commands = new List<PrioritizedRenderCommand>();
            float totalEstimatedTime = 0f;

            lock (_queueLock) {
                while (commands.Count < maxCommands && totalEstimatedTime < maxEstimatedTimeMs) {
                    var command = DequeueHighestPriorityCommandInternal();
                    if (command == null) break;

                    if (totalEstimatedTime + command.EstimatedExecutionTimeMs <= maxEstimatedTimeMs) {
                        commands.Add(command);
                        totalEstimatedTime += command.EstimatedExecutionTimeMs;
                    } else {
                        // Put the command back if it would exceed the time limit
                        _priorityQueues[command.Priority].Enqueue(command);
                        break;
                    }
                }
            }

            // Only log when there are actually commands processed
            if (commands.Count > 0) {
                Logger.Debug($"Dequeued {commands.Count} commands with total estimated time {totalEstimatedTime:F1}ms");
            }
            return commands;
        }

        private PrioritizedRenderCommand DequeueHighestPriorityCommandInternal() {
            // Process priorities from highest to lowest
            foreach (RenderCommandPriority priority in Enum.GetValues(typeof(RenderCommandPriority)).Cast<RenderCommandPriority>().OrderByDescending(p => p)) {
                if (_priorityQueues[priority].Count > 0) {
                    return _priorityQueues[priority].Dequeue();
                }
            }
            return null;
        }

        /// <summary>
        /// Records execution statistics for performance tracking.
        /// </summary>
        /// <param name="stats">The execution statistics to record.</param>
        public void RecordExecutionStats(CommandExecutionStats stats) {
            if (stats == null) return;

            lock (_queueLock) {
                _totalExecutionTimes[stats.Priority] += stats.ActualExecutionTimeMs;
                _executionCounts[stats.Priority]++;
            }
        }

        /// <summary>
        /// Gets comprehensive performance statistics for all priority levels.
        /// </summary>
        /// <returns>A string containing detailed performance information.</returns>
        public string GetPerformanceStatistics() {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== Render Command Priority Manager Statistics ===");

            lock (_queueLock) {
                stats.AppendLine($"Total Queued Commands: {TotalQueuedCommands}");
                stats.AppendLine("Priority Level Breakdown:");

                foreach (RenderCommandPriority priority in Enum.GetValues(typeof(RenderCommandPriority)).Cast<RenderCommandPriority>().OrderByDescending(p => p)) {
                    var queuedCount = _priorityQueues[priority].Count;
                    var executedCount = _executionCounts[priority];
                    var totalTime = _totalExecutionTimes[priority];
                    var avgTime = executedCount > 0 ? (double)totalTime / executedCount : 0.0;

                    stats.AppendLine($"  └─ {priority}: {queuedCount} queued, {executedCount} executed, {avgTime:F2}ms avg");
                }
            }

            stats.AppendLine("===================================================");
            return stats.ToString();
        }

        /// <summary>
        /// Clears all queued commands and resets performance statistics.
        /// </summary>
        public void Clear() {
            lock (_queueLock) {
                foreach (var queue in _priorityQueues.Values) {
                    queue.Clear();
                }

                foreach (var priority in _totalExecutionTimes.Keys.ToList()) {
                    _totalExecutionTimes[priority] = 0;
                    _executionCounts[priority] = 0;
                }
            }

            Logger.Debug("Render command priority manager cleared.");
        }
    }
}
