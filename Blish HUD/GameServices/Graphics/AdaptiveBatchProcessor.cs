using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;

namespace Blish_HUD.Graphics {
    
    /// <summary>
    /// Provides adaptive batch processing for render commands with dynamic sizing
    /// based on current system load and performance characteristics.
    /// </summary>
    public class AdaptiveBatchProcessor {
        private static readonly Logger Logger = Logger.GetLogger<AdaptiveBatchProcessor>();

        // Configuration constants
        private const float TARGET_FRAME_TIME_MS = 16.67f; // 60 FPS target
        private const int BASE_BATCH_SIZE = 8;
        private const int MIN_BATCH_SIZE = 1;
        private const int MAX_BATCH_SIZE = 64;
        private const float LOAD_ADAPTATION_FACTOR = 0.8f;
        private const int PERFORMANCE_HISTORY_SIZE = 30;

        private readonly RenderCommandPriorityManager _priorityManager;
        private readonly RenderBatchPool _batchPool;
        private readonly object _processingLock = new object();

        // Adaptive sizing state
        private int _currentOptimalBatchSize = BASE_BATCH_SIZE;
        private int _lastLoggedBatchSize = -1;
        private readonly Queue<float> _recentFrameTimes = new Queue<float>();
        private readonly Queue<BatchPerformanceData> _batchPerformanceHistory = new Queue<BatchPerformanceData>();
        
        // Performance monitoring
        private long _totalBatchesProcessed = 0;
        private long _totalCommandsProcessed = 0;
        private long _totalProcessingTimeMs = 0;
        private float _averageFrameTime = TARGET_FRAME_TIME_MS;
        private readonly Stopwatch _performanceTimer = Stopwatch.StartNew();

        /// <summary>
        /// Gets the current optimal batch size based on recent performance data.
        /// </summary>
        public int CurrentOptimalBatchSize => _currentOptimalBatchSize;

        /// <summary>
        /// Gets the average frame time over recent frames.
        /// </summary>
        public float AverageFrameTime => _averageFrameTime;

        /// <summary>
        /// Gets the total number of batches processed.
        /// </summary>
        public long TotalBatchesProcessed => Interlocked.Read(ref _totalBatchesProcessed);

        /// <summary>
        /// Gets the total number of commands processed.
        /// </summary>
        public long TotalCommandsProcessed => Interlocked.Read(ref _totalCommandsProcessed);

        /// <summary>
        /// Gets the total processing time in milliseconds.
        /// </summary>
        public long TotalProcessingTimeMs => Interlocked.Read(ref _totalProcessingTimeMs);

        /// <summary>
        /// Gets the processing efficiency (commands per millisecond).
        /// </summary>
        public float ProcessingEfficiency {
            get {
                var totalTime = Interlocked.Read(ref _totalProcessingTimeMs);
                var totalCommands = Interlocked.Read(ref _totalCommandsProcessed);
                return totalTime > 0 ? (float)totalCommands / totalTime : 0f;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AdaptiveBatchProcessor class.
        /// </summary>
        /// <param name="priorityManager">The priority manager for render commands.</param>
        /// <param name="batchPool">The batch pool for reusing batch objects.</param>
        public AdaptiveBatchProcessor(RenderCommandPriorityManager priorityManager, RenderBatchPool batchPool) {
            _priorityManager = priorityManager ?? throw new ArgumentNullException(nameof(priorityManager));
            _batchPool = batchPool ?? throw new ArgumentNullException(nameof(batchPool));

            Logger.Debug($"AdaptiveBatchProcessor initialized with base batch size {BASE_BATCH_SIZE}.");
        }

        /// <summary>
        /// Updates the frame time information for adaptive batch sizing.
        /// </summary>
        /// <param name="frameTimeMs">The current frame time in milliseconds.</param>
        public void UpdateFrameTime(float frameTimeMs) {
            lock (_processingLock) {
                _recentFrameTimes.Enqueue(frameTimeMs);
                
                // Keep only recent frame times
                while (_recentFrameTimes.Count > PERFORMANCE_HISTORY_SIZE) {
                    _recentFrameTimes.Dequeue();
                }

                // Calculate average frame time
                float total = 0f;
                foreach (var time in _recentFrameTimes) {
                    total += time;
                }
                _averageFrameTime = _recentFrameTimes.Count > 0 ? total / _recentFrameTimes.Count : TARGET_FRAME_TIME_MS;

                // Adapt batch size based on performance
                AdaptBatchSize();
            }
        }

        /// <summary>
        /// Processes render commands using adaptive batching with the specified graphics device.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device to use for rendering.</param>
        /// <param name="maxProcessingTimeMs">Maximum time to spend processing commands.</param>
        /// <returns>Processing statistics for this batch processing session.</returns>
        public BatchProcessingStats ProcessCommands(GraphicsDevice graphicsDevice, float maxProcessingTimeMs = TARGET_FRAME_TIME_MS) {
            if (graphicsDevice == null) {
                throw new ArgumentNullException(nameof(graphicsDevice));
            }

            var processingTimer = Stopwatch.StartNew();
            var stats = new BatchProcessingStats();
            var processedBatches = new List<RenderBatch>();

            try {
                while (processingTimer.ElapsedMilliseconds < maxProcessingTimeMs) {
                    // Calculate remaining time budget
                    var remainingTimeMs = maxProcessingTimeMs - processingTimer.ElapsedMilliseconds;
                    if (remainingTimeMs < 1f) break;

                    // Create and fill a batch with commands
                    var batch = CreateOptimalBatch(remainingTimeMs);
                    if (batch == null || batch.IsEmpty) {
                        _batchPool.ReturnBatch(batch);
                        break; // No more commands to process
                    }

                    // Execute the batch
                    var batchTimer = Stopwatch.StartNew();
                    var executedCommands = batch.Execute(graphicsDevice);
                    batchTimer.Stop();

                    // Record batch performance
                    var batchPerformance = new BatchPerformanceData(
                        batch.BatchId,
                        batch.CommandCount,
                        executedCommands,
                        batchTimer.ElapsedMilliseconds,
                        DateTime.UtcNow
                    );

                    RecordBatchPerformance(batchPerformance);
                    processedBatches.Add(batch);

                    // Update statistics
                    stats.BatchesProcessed++;
                    stats.CommandsProcessed += executedCommands;
                    stats.TotalProcessingTimeMs += batchTimer.ElapsedMilliseconds;

                    // Check if we should continue processing
                    if (executedCommands == 0) break; // No commands were executed
                }
            } finally {
                processingTimer.Stop();
                stats.TotalElapsedTimeMs = processingTimer.ElapsedMilliseconds;

                // Return batches to pool
                foreach (var batch in processedBatches) {
                    _batchPool.ReturnBatch(batch);
                }

                // Update global statistics
                Interlocked.Add(ref _totalBatchesProcessed, stats.BatchesProcessed);
                Interlocked.Add(ref _totalCommandsProcessed, stats.CommandsProcessed);
                Interlocked.Add(ref _totalProcessingTimeMs, stats.TotalProcessingTimeMs);
            }

            // Only log when there are actually batches processed
            if (stats.BatchesProcessed > 0) {
                Logger.Debug($"Processed {stats.BatchesProcessed} batches with {stats.CommandsProcessed} commands in {stats.TotalElapsedTimeMs}ms");
            }
            return stats;
        }

        /// <summary>
        /// Creates an optimally-sized batch based on current performance characteristics.
        /// </summary>
        /// <param name="remainingTimeMs">The remaining time budget for processing.</param>
        /// <returns>A batch filled with commands, or null if no commands are available.</returns>
        private RenderBatch CreateOptimalBatch(float remainingTimeMs) {
            var batch = _batchPool.AcquireBatch();
            var maxCommands = CalculateOptimalBatchSize(remainingTimeMs);
            var commands = _priorityManager.DequeueCommands(maxCommands, remainingTimeMs);

            if (commands.Count == 0) {
                return batch; // Return empty batch
            }

            // Add commands to batch
            foreach (var command in commands) {
                if (!batch.TryAddCommand(command.Command)) {
                    // Batch is full, put remaining commands back
                    for (int i = commands.IndexOf(command); i < commands.Count; i++) {
                        _priorityManager.EnqueueCommand(
                            commands[i].Priority,
                            commands[i].Command,
                            commands[i].EstimatedExecutionTimeMs,
                            commands[i].Metadata,
                            commands[i].CanBatch
                        );
                    }
                    break;
                }
            }

            return batch;
        }

        /// <summary>
        /// Calculates the optimal batch size based on current performance and remaining time.
        /// </summary>
        /// <param name="remainingTimeMs">The remaining time budget for processing.</param>
        /// <returns>The optimal number of commands for the next batch.</returns>
        private int CalculateOptimalBatchSize(float remainingTimeMs) {
            lock (_processingLock) {
                // Base calculation on current optimal size
                int baseSize = _currentOptimalBatchSize;

                // Adjust based on remaining time
                float timeRatio = remainingTimeMs / TARGET_FRAME_TIME_MS;
                int timeAdjustedSize = (int)(baseSize * Math.Max(0.1f, Math.Min(2.0f, timeRatio)));

                // Adjust based on queue length
                int queueLength = _priorityManager.TotalQueuedCommands;
                if (queueLength > baseSize * 2) {
                    // Increase batch size when queue is long
                    timeAdjustedSize = (int)(timeAdjustedSize * 1.5f);
                } else if (queueLength < baseSize / 2) {
                    // Decrease batch size when queue is short
                    timeAdjustedSize = Math.Max(1, timeAdjustedSize / 2);
                }

                // Clamp to reasonable bounds
                return Math.Max(MIN_BATCH_SIZE, Math.Min(MAX_BATCH_SIZE, timeAdjustedSize));
            }
        }

        /// <summary>
        /// Adapts the optimal batch size based on recent performance data.
        /// </summary>
        private void AdaptBatchSize() {
            if (_batchPerformanceHistory.Count < 5) return; // Need some history

            // Analyze recent batch performance
            float avgBatchTime = 0f;
            float avgCommandsPerBatch = 0f;
            int sampleCount = 0;

            foreach (var performance in _batchPerformanceHistory) {
                avgBatchTime += performance.ProcessingTimeMs;
                avgCommandsPerBatch += performance.CommandCount;
                sampleCount++;
            }

            if (sampleCount == 0) return;

            avgBatchTime /= sampleCount;
            avgCommandsPerBatch /= sampleCount;

            // Calculate efficiency metrics
            float commandsPerMs = avgCommandsPerBatch / Math.Max(0.1f, avgBatchTime);
            float targetCommandsPerMs = BASE_BATCH_SIZE / (TARGET_FRAME_TIME_MS * 0.25f); // Target 25% of frame time

            // Adjust batch size based on efficiency
            if (_averageFrameTime > TARGET_FRAME_TIME_MS * 1.1f) {
                // Frame time too high, reduce batch size
                _currentOptimalBatchSize = Math.Max(MIN_BATCH_SIZE, (int)(_currentOptimalBatchSize * LOAD_ADAPTATION_FACTOR));
            } else if (_averageFrameTime < TARGET_FRAME_TIME_MS * 0.8f && commandsPerMs > targetCommandsPerMs) {
                // Frame time good and efficient, can increase batch size
                _currentOptimalBatchSize = Math.Min(MAX_BATCH_SIZE, (int)(_currentOptimalBatchSize * (1f + (1f - LOAD_ADAPTATION_FACTOR))));
            }

            // Only log batch size changes, not every adaptation attempt
            if (_currentOptimalBatchSize != _lastLoggedBatchSize) {
                Logger.Debug($"Adapted batch size to {_currentOptimalBatchSize} (avg frame: {_averageFrameTime:F2}ms, efficiency: {commandsPerMs:F2} cmd/ms)");
                _lastLoggedBatchSize = _currentOptimalBatchSize;
            }
        }

        /// <summary>
        /// Records batch performance data for adaptive sizing.
        /// </summary>
        /// <param name="performance">The batch performance data to record.</param>
        private void RecordBatchPerformance(BatchPerformanceData performance) {
            lock (_processingLock) {
                _batchPerformanceHistory.Enqueue(performance);

                // Keep only recent performance data
                while (_batchPerformanceHistory.Count > PERFORMANCE_HISTORY_SIZE) {
                    _batchPerformanceHistory.Dequeue();
                }
            }
        }

        /// <summary>
        /// Gets comprehensive performance statistics for the adaptive batch processor.
        /// </summary>
        /// <returns>A string containing detailed performance information.</returns>
        public string GetPerformanceStatistics() {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== Adaptive Batch Processor Statistics ===");
            
            lock (_processingLock) {
                stats.AppendLine($"Current Configuration:");
                stats.AppendLine($"  └─ Optimal Batch Size: {_currentOptimalBatchSize}");
                stats.AppendLine($"  └─ Average Frame Time: {_averageFrameTime:F2}ms");
                stats.AppendLine($"  └─ Target Frame Time: {TARGET_FRAME_TIME_MS:F2}ms");
                
                stats.AppendLine($"Performance Metrics:");
                stats.AppendLine($"  └─ Total Batches Processed: {TotalBatchesProcessed}");
                stats.AppendLine($"  └─ Total Commands Processed: {TotalCommandsProcessed}");
                stats.AppendLine($"  └─ Total Processing Time: {TotalProcessingTimeMs}ms");
                stats.AppendLine($"  └─ Processing Efficiency: {ProcessingEfficiency:F2} commands/ms");
                stats.AppendLine($"  └─ Uptime: {_performanceTimer.Elapsed.TotalSeconds:F1}s");
                
                if (TotalBatchesProcessed > 0) {
                    var avgCommandsPerBatch = (float)TotalCommandsProcessed / TotalBatchesProcessed;
                    var avgProcessingTimePerBatch = (float)TotalProcessingTimeMs / TotalBatchesProcessed;
                    stats.AppendLine($"  └─ Avg Commands/Batch: {avgCommandsPerBatch:F1}");
                    stats.AppendLine($"  └─ Avg Processing Time/Batch: {avgProcessingTimePerBatch:F2}ms");
                }
            }
            
            stats.AppendLine("===========================================");
            return stats.ToString();
        }

        /// <summary>
        /// Resets all performance counters and adaptive sizing state.
        /// </summary>
        public void ResetPerformanceCounters() {
            lock (_processingLock) {
                Interlocked.Exchange(ref _totalBatchesProcessed, 0);
                Interlocked.Exchange(ref _totalCommandsProcessed, 0);
                Interlocked.Exchange(ref _totalProcessingTimeMs, 0);
                
                _recentFrameTimes.Clear();
                _batchPerformanceHistory.Clear();
                _currentOptimalBatchSize = BASE_BATCH_SIZE;
                _averageFrameTime = TARGET_FRAME_TIME_MS;
                _performanceTimer.Restart();
            }
            
            Logger.Debug("Adaptive batch processor performance counters reset.");
        }
    }

    /// <summary>
    /// Contains performance data for a processed batch.
    /// </summary>
    public class BatchPerformanceData {
        /// <summary>
        /// Gets the batch ID.
        /// </summary>
        public int BatchId { get; }
        
        /// <summary>
        /// Gets the number of commands in the batch.
        /// </summary>
        public int CommandCount { get; }
        
        /// <summary>
        /// Gets the number of commands successfully executed.
        /// </summary>
        public int ExecutedCommands { get; }
        
        /// <summary>
        /// Gets the processing time in milliseconds.
        /// </summary>
        public long ProcessingTimeMs { get; }
        
        /// <summary>
        /// Gets the timestamp when the batch was processed.
        /// </summary>
        public DateTime ProcessedAt { get; }

        /// <summary>
        /// Initializes a new instance of the BatchPerformanceData class.
        /// </summary>
        public BatchPerformanceData(int batchId, int commandCount, int executedCommands, long processingTimeMs, DateTime processedAt) {
            BatchId = batchId;
            CommandCount = commandCount;
            ExecutedCommands = executedCommands;
            ProcessingTimeMs = processingTimeMs;
            ProcessedAt = processedAt;
        }
    }

    /// <summary>
    /// Contains statistics for a batch processing session.
    /// </summary>
    public class BatchProcessingStats {
        /// <summary>
        /// Gets or sets the number of batches processed.
        /// </summary>
        public int BatchesProcessed { get; set; }
        
        /// <summary>
        /// Gets or sets the total number of commands processed.
        /// </summary>
        public int CommandsProcessed { get; set; }
        
        /// <summary>
        /// Gets or sets the total processing time in milliseconds.
        /// </summary>
        public long TotalProcessingTimeMs { get; set; }
        
        /// <summary>
        /// Gets or sets the total elapsed time in milliseconds.
        /// </summary>
        public long TotalElapsedTimeMs { get; set; }

        /// <summary>
        /// Gets the processing efficiency (commands per millisecond).
        /// </summary>
        public float ProcessingEfficiency => TotalProcessingTimeMs > 0 ? (float)CommandsProcessed / TotalProcessingTimeMs : 0f;

        /// <summary>
        /// Gets the utilization ratio (processing time / elapsed time).
        /// </summary>
        public float UtilizationRatio => TotalElapsedTimeMs > 0 ? (float)TotalProcessingTimeMs / TotalElapsedTimeMs : 0f;

        /// <summary>
        /// Gets a string representation of these processing statistics.
        /// </summary>
        public override string ToString() {
            return $"Processed {BatchesProcessed} batches, {CommandsProcessed} commands in {TotalElapsedTimeMs}ms " +
                   $"(efficiency: {ProcessingEfficiency:F2} cmd/ms, utilization: {UtilizationRatio:P1})";
        }
    }
}
