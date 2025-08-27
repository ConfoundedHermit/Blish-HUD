using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;

namespace Blish_HUD._Utils {
    /// <summary>
    /// Provides a thread-safe pool of StringBuilder instances to reduce allocations
    /// and improve performance in string concatenation operations.
    /// </summary>
    public static class StringBuilderPool {
        private static readonly ConcurrentQueue<StringBuilder> _pool = new ConcurrentQueue<StringBuilder>();
        private static readonly int _maxPoolSize = 50;
        private static readonly int _defaultCapacity = 256;
        private static int _currentCount = 0;

        // Performance tracking
        private static long _totalGets = 0;
        private static long _totalReturns = 0;
        private static long _poolHits = 0;
        private static long _poolMisses = 0;

        /// <summary>
        /// Gets a StringBuilder from the pool or creates a new one if the pool is empty.
        /// </summary>
        /// <param name="capacity">The initial capacity for the StringBuilder. If -1, uses default capacity.</param>
        /// <returns>A StringBuilder instance ready for use.</returns>
        public static StringBuilder Get(int capacity = -1) {
            Interlocked.Increment(ref _totalGets);

            if (_pool.TryDequeue(out StringBuilder sb)) {
                Interlocked.Decrement(ref _currentCount);
                Interlocked.Increment(ref _poolHits);

                // Ensure the StringBuilder has adequate capacity
                if (capacity > 0 && sb.Capacity < capacity) {
                    sb.Capacity = capacity;
                }
                return sb;
            }

            Interlocked.Increment(ref _poolMisses);
            return new StringBuilder(capacity > 0 ? capacity : _defaultCapacity);
        }

        /// <summary>
        /// Returns a StringBuilder to the pool for reuse. The StringBuilder is cleared before being pooled.
        /// </summary>
        /// <param name="sb">The StringBuilder to return to the pool.</param>
        public static void Return(StringBuilder sb) {
            if (sb == null) return;

            Interlocked.Increment(ref _totalReturns);

            // Only pool if we haven't exceeded the maximum pool size
            if (_currentCount < _maxPoolSize) {
                sb.Clear();
                
                // Reset capacity if it's grown too large to avoid memory bloat
                if (sb.Capacity > 4096) {
                    sb.Capacity = _defaultCapacity;
                }

                _pool.Enqueue(sb);
                Interlocked.Increment(ref _currentCount);
            }
            // If pool is full, let the StringBuilder be garbage collected
        }

        /// <summary>
        /// Gets performance statistics for the StringBuilder pool.
        /// </summary>
        /// <returns>A formatted string containing pool performance statistics.</returns>
        public static string GetPerformanceStatistics() {
            var hitRate = _totalGets > 0 ? (_poolHits * 100.0 / _totalGets) : 0;
            return $"StringBuilder Pool Stats - Pool Size: {_currentCount}/{_maxPoolSize}, " +
                   $"Total Gets: {_totalGets}, Returns: {_totalReturns}, " +
                   $"Hit Rate: {hitRate:F1}% ({_poolHits} hits, {_poolMisses} misses)";
        }

        /// <summary>
        /// Resets the performance counters for the StringBuilder pool.
        /// </summary>
        public static void ResetPerformanceCounters() {
            Interlocked.Exchange(ref _totalGets, 0);
            Interlocked.Exchange(ref _totalReturns, 0);
            Interlocked.Exchange(ref _poolHits, 0);
            Interlocked.Exchange(ref _poolMisses, 0);
        }

        /// <summary>
        /// Gets the current number of StringBuilders in the pool.
        /// </summary>
        public static int PoolSize => _currentCount;

        /// <summary>
        /// Gets the maximum pool size.
        /// </summary>
        public static int MaxPoolSize => _maxPoolSize;

        /// <summary>
        /// Gets the pool hit rate as a percentage.
        /// </summary>
        public static double HitRate => _totalGets > 0 ? (_poolHits * 100.0 / _totalGets) : 0;
    }
}
