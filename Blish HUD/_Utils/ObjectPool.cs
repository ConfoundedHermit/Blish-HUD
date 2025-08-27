using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Blish_HUD._Utils {
    /// <summary>
    /// Provides a thread-safe generic object pool to reduce allocations and improve performance.
    /// Objects are pooled and reused to minimize garbage collection pressure.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool. Must be a reference type with a parameterless constructor.</typeparam>
    public class ObjectPool<T> where T : class, new() {
        private readonly ConcurrentQueue<T> _objects = new ConcurrentQueue<T>();
        private readonly Func<T> _objectGenerator;
        private readonly Action<T> _resetAction;
        private readonly Func<T, bool> _validateAction;
        private readonly int _maxSize;
        private int _currentCount = 0;

        // Performance tracking
        private long _totalGets = 0;
        private long _totalReturns = 0;
        private long _poolHits = 0;
        private long _poolMisses = 0;
        private long _validationFailures = 0;

        /// <summary>
        /// Initializes a new instance of the ObjectPool class.
        /// </summary>
        /// <param name="objectGenerator">Function to create new instances. If null, uses default constructor.</param>
        /// <param name="resetAction">Action to reset object state before returning to pool. Can be null.</param>
        /// <param name="validateAction">Function to validate object state before reuse. Can be null.</param>
        /// <param name="maxSize">Maximum number of objects to keep in the pool. Default is 50.</param>
        public ObjectPool(Func<T> objectGenerator = null, Action<T> resetAction = null, Func<T, bool> validateAction = null, int maxSize = 50) {
            _objectGenerator = objectGenerator ?? (() => new T());
            _resetAction = resetAction;
            _validateAction = validateAction;
            _maxSize = maxSize;
        }

        /// <summary>
        /// Gets an object from the pool or creates a new one if the pool is empty.
        /// </summary>
        /// <returns>An object instance ready for use.</returns>
        public T Get() {
            Interlocked.Increment(ref _totalGets);

            while (_objects.TryDequeue(out T item)) {
                Interlocked.Decrement(ref _currentCount);

                // Validate the object if validation is configured
                if (_validateAction != null && !_validateAction(item)) {
                    Interlocked.Increment(ref _validationFailures);
                    continue; // Try next object in pool
                }

                Interlocked.Increment(ref _poolHits);
                return item;
            }

            // Pool is empty or all objects failed validation, create new instance
            Interlocked.Increment(ref _poolMisses);
            return _objectGenerator();
        }

        /// <summary>
        /// Returns an object to the pool for reuse. The object is reset before being pooled.
        /// </summary>
        /// <param name="item">The object to return to the pool.</param>
        public void Return(T item) {
            if (item == null) return;

            Interlocked.Increment(ref _totalReturns);

            // Only pool if we haven't exceeded the maximum pool size
            if (_currentCount < _maxSize) {
                try {
                    // Reset the object state if reset action is configured
                    _resetAction?.Invoke(item);

                    _objects.Enqueue(item);
                    Interlocked.Increment(ref _currentCount);
                } catch {
                    // If reset fails, don't pool the object to avoid corruption
                    // Let it be garbage collected instead
                }
            }
            // If pool is full, let the object be garbage collected
        }

        /// <summary>
        /// Gets performance statistics for the object pool.
        /// </summary>
        /// <returns>A formatted string containing pool performance statistics.</returns>
        public string GetPerformanceStatistics() {
            var hitRate = _totalGets > 0 ? (_poolHits * 100.0 / _totalGets) : 0;
            var validationFailureRate = _totalGets > 0 ? (_validationFailures * 100.0 / _totalGets) : 0;
            
            return $"{typeof(T).Name} Pool Stats - Pool Size: {_currentCount}/{_maxSize}, " +
                   $"Total Gets: {_totalGets}, Returns: {_totalReturns}, " +
                   $"Hit Rate: {hitRate:F1}% ({_poolHits} hits, {_poolMisses} misses), " +
                   $"Validation Failures: {_validationFailures} ({validationFailureRate:F1}%)";
        }

        /// <summary>
        /// Resets the performance counters for the object pool.
        /// </summary>
        public void ResetPerformanceCounters() {
            Interlocked.Exchange(ref _totalGets, 0);
            Interlocked.Exchange(ref _totalReturns, 0);
            Interlocked.Exchange(ref _poolHits, 0);
            Interlocked.Exchange(ref _poolMisses, 0);
            Interlocked.Exchange(ref _validationFailures, 0);
        }

        /// <summary>
        /// Gets the current number of objects in the pool.
        /// </summary>
        public int PoolSize => _currentCount;

        /// <summary>
        /// Gets the maximum pool size.
        /// </summary>
        public int MaxPoolSize => _maxSize;

        /// <summary>
        /// Gets the pool hit rate as a percentage.
        /// </summary>
        public double HitRate => _totalGets > 0 ? (_poolHits * 100.0 / _totalGets) : 0;

        /// <summary>
        /// Gets the validation failure rate as a percentage.
        /// </summary>
        public double ValidationFailureRate => _totalGets > 0 ? (_validationFailures * 100.0 / _totalGets) : 0;

        /// <summary>
        /// Clears all objects from the pool. Use with caution.
        /// </summary>
        public void Clear() {
            while (_objects.TryDequeue(out _)) {
                Interlocked.Decrement(ref _currentCount);
            }
        }
    }

    /// <summary>
    /// Provides a convenient wrapper for using pooled objects with automatic return to pool.
    /// Implements IDisposable to ensure objects are returned to the pool when disposed.
    /// </summary>
    /// <typeparam name="T">The type of the pooled object.</typeparam>
    public struct PooledObject<T> : IDisposable where T : class, new() {
        private readonly ObjectPool<T> _pool;
        private readonly T _item;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the PooledObject struct.
        /// </summary>
        /// <param name="pool">The object pool that owns this object.</param>
        /// <param name="item">The pooled object instance.</param>
        internal PooledObject(ObjectPool<T> pool, T item) {
            _pool = pool;
            _item = item;
            _disposed = false;
        }

        /// <summary>
        /// Gets the pooled object instance.
        /// </summary>
        public T Value => _disposed ? throw new ObjectDisposedException(nameof(PooledObject<T>)) : _item;

        /// <summary>
        /// Returns the object to the pool. Called automatically when the PooledObject is disposed.
        /// </summary>
        public void Dispose() {
            if (!_disposed && _item != null) {
                _pool.Return(_item);
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Extension methods for ObjectPool to provide convenient usage patterns.
    /// </summary>
    public static class ObjectPoolExtensions {
        /// <summary>
        /// Gets a pooled object wrapped in a PooledObject struct that automatically returns the object to the pool when disposed.
        /// </summary>
        /// <typeparam name="T">The type of the pooled object.</typeparam>
        /// <param name="pool">The object pool to get the object from.</param>
        /// <returns>A PooledObject that automatically returns the object to the pool when disposed.</returns>
        public static PooledObject<T> GetPooled<T>(this ObjectPool<T> pool) where T : class, new() {
            return new PooledObject<T>(pool, pool.Get());
        }
    }
}
