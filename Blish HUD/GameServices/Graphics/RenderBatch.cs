using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;

namespace Blish_HUD.Graphics {
    
    /// <summary>
    /// Represents a batch of render commands that can be processed together for improved performance.
    /// </summary>
    public class RenderBatch {
        private static readonly Logger Logger = Logger.GetLogger<RenderBatch>();

        private readonly List<Action<GraphicsDevice>> _commands;
        private readonly Stopwatch _processingTimer;
        private readonly int _batchId;
        
        /// <summary>
        /// Gets the unique identifier for this batch.
        /// </summary>
        public int BatchId => _batchId;
        
        /// <summary>
        /// Gets the number of commands in this batch.
        /// </summary>
        public int CommandCount => _commands.Count;
        
        /// <summary>
        /// Gets the maximum number of commands this batch can hold.
        /// </summary>
        public int MaxCommands { get; }
        
        /// <summary>
        /// Gets a value indicating whether this batch is full.
        /// </summary>
        public bool IsFull => _commands.Count >= MaxCommands;
        
        /// <summary>
        /// Gets a value indicating whether this batch is empty.
        /// </summary>
        public bool IsEmpty => _commands.Count == 0;
        
        /// <summary>
        /// Gets the time when this batch was created.
        /// </summary>
        public DateTime CreatedAt { get; }
        
        /// <summary>
        /// Gets the total processing time for this batch in milliseconds.
        /// </summary>
        public long ProcessingTimeMs => _processingTimer.ElapsedMilliseconds;

        /// <summary>
        /// Initializes a new instance of the RenderBatch class.
        /// </summary>
        /// <param name="batchId">The unique identifier for this batch.</param>
        /// <param name="maxCommands">The maximum number of commands this batch can hold.</param>
        public RenderBatch(int batchId, int maxCommands = 32) {
            _batchId = batchId;
            MaxCommands = Math.Max(1, maxCommands);
            _commands = new List<Action<GraphicsDevice>>(MaxCommands);
            _processingTimer = new Stopwatch();
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Attempts to add a render command to this batch.
        /// </summary>
        /// <param name="command">The render command to add.</param>
        /// <returns>True if the command was added; false if the batch is full.</returns>
        public bool TryAddCommand(Action<GraphicsDevice> command) {
            if (command == null) {
                throw new ArgumentNullException(nameof(command));
            }

            if (IsFull) {
                return false;
            }

            _commands.Add(command);
            return true;
        }

        /// <summary>
        /// Executes all commands in this batch using the specified graphics device.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device to use for rendering.</param>
        /// <returns>The number of commands successfully executed.</returns>
        public int Execute(GraphicsDevice graphicsDevice) {
            if (graphicsDevice == null) {
                throw new ArgumentNullException(nameof(graphicsDevice));
            }

            if (IsEmpty) {
                return 0;
            }

            _processingTimer.Start();
            int executedCount = 0;

            try {
                foreach (var command in _commands) {
                    try {
                        command.Invoke(graphicsDevice);
                        executedCount++;
                    } catch (Exception ex) {
                        Logger.Warn(ex, $"Error executing render command in batch {_batchId}.");
                        // Continue executing other commands even if one fails
                    }
                }
            } finally {
                _processingTimer.Stop();
            }

            Logger.Debug($"Batch {_batchId} executed {executedCount}/{_commands.Count} commands in {ProcessingTimeMs}ms.");
            return executedCount;
        }

        /// <summary>
        /// Clears all commands from this batch, making it ready for reuse.
        /// </summary>
        public void Clear() {
            _commands.Clear();
            _processingTimer.Reset();
        }

        /// <summary>
        /// Gets performance statistics for this batch.
        /// </summary>
        /// <returns>A string containing batch performance information.</returns>
        public string GetPerformanceInfo() {
            var age = DateTime.UtcNow - CreatedAt;
            return $"Batch {_batchId}: {_commands.Count}/{MaxCommands} commands, {ProcessingTimeMs}ms processing, {age.TotalMilliseconds:F1}ms age";
        }
    }

    /// <summary>
    /// Manages the creation and reuse of render batches for optimal performance.
    /// </summary>
    public class RenderBatchPool {
        private static readonly Logger Logger = Logger.GetLogger<RenderBatchPool>();

        private readonly Queue<RenderBatch> _availableBatches;
        private readonly object _poolLock = new object();
        private int _nextBatchId = 1;
        private readonly int _maxBatchSize;

        /// <summary>
        /// Gets the number of available batches in the pool.
        /// </summary>
        public int AvailableBatchCount {
            get {
                lock (_poolLock) {
                    return _availableBatches.Count;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the RenderBatchPool class.
        /// </summary>
        /// <param name="maxBatchSize">The maximum size for batches created by this pool.</param>
        public RenderBatchPool(int maxBatchSize = 32) {
            _maxBatchSize = Math.Max(1, maxBatchSize);
            _availableBatches = new Queue<RenderBatch>();
            
            // Pre-create a few batches for immediate use
            for (int i = 0; i < 4; i++) {
                _availableBatches.Enqueue(CreateNewBatch());
            }
            
            Logger.Debug($"RenderBatchPool initialized with {_availableBatches.Count} pre-created batches (max size: {_maxBatchSize}).");
        }

        /// <summary>
        /// Acquires a batch from the pool, creating a new one if necessary.
        /// </summary>
        /// <returns>A render batch ready for use.</returns>
        public RenderBatch AcquireBatch() {
            lock (_poolLock) {
                if (_availableBatches.Count > 0) {
                    var batch = _availableBatches.Dequeue();
                    batch.Clear(); // Ensure the batch is clean
                    return batch;
                }
            }

            // Create a new batch if none are available
            return CreateNewBatch();
        }

        /// <summary>
        /// Returns a batch to the pool for reuse.
        /// </summary>
        /// <param name="batch">The batch to return to the pool.</param>
        public void ReturnBatch(RenderBatch batch) {
            if (batch == null) return;

            batch.Clear();

            lock (_poolLock) {
                // Limit pool size to prevent excessive memory usage
                if (_availableBatches.Count < 16) {
                    _availableBatches.Enqueue(batch);
                }
            }
        }

        private RenderBatch CreateNewBatch() {
            var batchId = System.Threading.Interlocked.Increment(ref _nextBatchId);
            return new RenderBatch(batchId, _maxBatchSize);
        }
    }
}
