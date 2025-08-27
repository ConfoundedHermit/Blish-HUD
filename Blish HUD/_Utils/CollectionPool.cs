using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace Blish_HUD._Utils {
    /// <summary>
    /// Provides object pools for commonly used collection types to reduce allocations
    /// and improve performance in data processing operations.
    /// </summary>
    public static class CollectionPool {
        
        // Object pools for collection types
        private static readonly ObjectPool<List<object>> _objectListPool = new ObjectPool<List<object>>(
            objectGenerator: () => new List<object>(),
            resetAction: list => list.Clear(),
            maxSize: 100
        );

        private static readonly ObjectPool<List<string>> _stringListPool = new ObjectPool<List<string>>(
            objectGenerator: () => new List<string>(),
            resetAction: list => list.Clear(),
            maxSize: 50
        );

        private static readonly ObjectPool<Dictionary<string, object>> _stringObjectDictionaryPool = new ObjectPool<Dictionary<string, object>>(
            objectGenerator: () => new Dictionary<string, object>(),
            resetAction: dict => dict.Clear(),
            maxSize: 50
        );

        private static readonly ObjectPool<Dictionary<string, string>> _stringStringDictionaryPool = new ObjectPool<Dictionary<string, string>>(
            objectGenerator: () => new Dictionary<string, string>(),
            resetAction: dict => dict.Clear(),
            maxSize: 30
        );

        private static readonly ObjectPool<HashSet<string>> _stringHashSetPool = new ObjectPool<HashSet<string>>(
            objectGenerator: () => new HashSet<string>(),
            resetAction: set => set.Clear(),
            maxSize: 30
        );

        private static readonly ObjectPool<Queue<object>> _objectQueuePool = new ObjectPool<Queue<object>>(
            objectGenerator: () => new Queue<object>(),
            resetAction: queue => queue.Clear(),
            maxSize: 20
        );

        // Generic pools using concurrent dictionaries for thread safety
        private static readonly ConcurrentDictionary<Type, object> _genericListPools = new ConcurrentDictionary<Type, object>();
        private static readonly ConcurrentDictionary<(Type, Type), object> _genericDictionaryPools = new ConcurrentDictionary<(Type, Type), object>();

        #region List Pools

        /// <summary>
        /// Gets a List&lt;T&gt; from the appropriate pool or creates a new one if the pool is empty.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="capacity">The initial capacity for the list. If -1, uses default capacity.</param>
        /// <returns>A List&lt;T&gt; instance ready for use.</returns>
        public static List<T> GetList<T>(int capacity = -1) {
            var pool = GetOrCreateListPool<T>();
            var list = pool.Get();
            
            // Ensure adequate capacity if specified
            if (capacity > 0 && list.Capacity < capacity) {
                list.Capacity = capacity;
            }
            
            return list;
        }

        /// <summary>
        /// Returns a List&lt;T&gt; to the appropriate pool for reuse.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The List&lt;T&gt; to return to the pool.</param>
        public static void ReturnList<T>(List<T> list) {
            if (list == null) return;
            
            var pool = GetOrCreateListPool<T>();
            pool.Return(list);
        }

        /// <summary>
        /// Gets or creates a pool for List&lt;T&gt; of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <returns>An ObjectPool for List&lt;T&gt;.</returns>
        private static ObjectPool<List<T>> GetOrCreateListPool<T>() {
            var type = typeof(T);
            
            // Use specialized pools for common types
            if (type == typeof(object)) {
                return (ObjectPool<List<T>>)(object)_objectListPool;
            }
            if (type == typeof(string)) {
                return (ObjectPool<List<T>>)(object)_stringListPool;
            }
            
            // Get or create generic pool
            return (ObjectPool<List<T>>)_genericListPools.GetOrAdd(type, _ => 
                new ObjectPool<List<T>>(
                    objectGenerator: () => new List<T>(),
                    resetAction: list => list.Clear(),
                    maxSize: 30
                )
            );
        }

        #endregion

        #region Dictionary Pools

        /// <summary>
        /// Gets a Dictionary&lt;TKey, TValue&gt; from the appropriate pool or creates a new one if the pool is empty.
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
        /// <param name="capacity">The initial capacity for the dictionary. If -1, uses default capacity.</param>
        /// <returns>A Dictionary&lt;TKey, TValue&gt; instance ready for use.</returns>
        public static Dictionary<TKey, TValue> GetDictionary<TKey, TValue>(int capacity = -1) {
            var pool = GetOrCreateDictionaryPool<TKey, TValue>();
            var dictionary = pool.Get();
            
            // Note: Dictionary doesn't have a public Capacity setter, so we can't pre-size it
            // The capacity parameter is kept for API consistency and future use
            
            return dictionary;
        }

        /// <summary>
        /// Returns a Dictionary&lt;TKey, TValue&gt; to the appropriate pool for reuse.
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
        /// <param name="dictionary">The Dictionary&lt;TKey, TValue&gt; to return to the pool.</param>
        public static void ReturnDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary) {
            if (dictionary == null) return;
            
            var pool = GetOrCreateDictionaryPool<TKey, TValue>();
            pool.Return(dictionary);
        }

        /// <summary>
        /// Gets or creates a pool for Dictionary&lt;TKey, TValue&gt; of the specified types.
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
        /// <returns>An ObjectPool for Dictionary&lt;TKey, TValue&gt;.</returns>
        private static ObjectPool<Dictionary<TKey, TValue>> GetOrCreateDictionaryPool<TKey, TValue>() {
            var keyType = typeof(TKey);
            var valueType = typeof(TValue);
            
            // Use specialized pools for common types
            if (keyType == typeof(string) && valueType == typeof(object)) {
                return (ObjectPool<Dictionary<TKey, TValue>>)(object)_stringObjectDictionaryPool;
            }
            if (keyType == typeof(string) && valueType == typeof(string)) {
                return (ObjectPool<Dictionary<TKey, TValue>>)(object)_stringStringDictionaryPool;
            }
            
            // Get or create generic pool
            var poolKey = (keyType, valueType);
            return (ObjectPool<Dictionary<TKey, TValue>>)_genericDictionaryPools.GetOrAdd(poolKey, _ => 
                new ObjectPool<Dictionary<TKey, TValue>>(
                    objectGenerator: () => new Dictionary<TKey, TValue>(),
                    resetAction: dict => dict.Clear(),
                    maxSize: 20
                )
            );
        }

        #endregion

        #region HashSet Pools

        /// <summary>
        /// Gets a HashSet&lt;string&gt; from the pool or creates a new one if the pool is empty.
        /// </summary>
        /// <returns>A HashSet&lt;string&gt; instance ready for use.</returns>
        public static HashSet<string> GetStringHashSet() {
            return _stringHashSetPool.Get();
        }

        /// <summary>
        /// Returns a HashSet&lt;string&gt; to the pool for reuse.
        /// </summary>
        /// <param name="hashSet">The HashSet&lt;string&gt; to return to the pool.</param>
        public static void ReturnStringHashSet(HashSet<string> hashSet) {
            if (hashSet != null) {
                _stringHashSetPool.Return(hashSet);
            }
        }

        #endregion

        #region Queue Pools

        /// <summary>
        /// Gets a Queue&lt;object&gt; from the pool or creates a new one if the pool is empty.
        /// </summary>
        /// <returns>A Queue&lt;object&gt; instance ready for use.</returns>
        public static Queue<object> GetObjectQueue() {
            return _objectQueuePool.Get();
        }

        /// <summary>
        /// Returns a Queue&lt;object&gt; to the pool for reuse.
        /// </summary>
        /// <param name="queue">The Queue&lt;object&gt; to return to the pool.</param>
        public static void ReturnObjectQueue(Queue<object> queue) {
            if (queue != null) {
                _objectQueuePool.Return(queue);
            }
        }

        #endregion

        #region Pool Statistics and Management

        /// <summary>
        /// Gets performance statistics for all collection pools.
        /// </summary>
        /// <returns>A formatted string containing performance statistics for all pools.</returns>
        public static string GetPerformanceStatistics() {
            var stats = "Collection Pool Statistics:\n";
            stats += $"  Object List Pool - {_objectListPool.GetPerformanceStatistics()}\n";
            stats += $"  String List Pool - {_stringListPool.GetPerformanceStatistics()}\n";
            stats += $"  String-Object Dictionary Pool - {_stringObjectDictionaryPool.GetPerformanceStatistics()}\n";
            stats += $"  String-String Dictionary Pool - {_stringStringDictionaryPool.GetPerformanceStatistics()}\n";
            stats += $"  String HashSet Pool - {_stringHashSetPool.GetPerformanceStatistics()}\n";
            stats += $"  Object Queue Pool - {_objectQueuePool.GetPerformanceStatistics()}\n";
            stats += $"  Generic List Pools: {_genericListPools.Count} types\n";
            stats += $"  Generic Dictionary Pools: {_genericDictionaryPools.Count} type combinations";
            
            return stats;
        }

        /// <summary>
        /// Resets performance counters for all collection pools.
        /// </summary>
        public static void ResetPerformanceCounters() {
            _objectListPool.ResetPerformanceCounters();
            _stringListPool.ResetPerformanceCounters();
            _stringObjectDictionaryPool.ResetPerformanceCounters();
            _stringStringDictionaryPool.ResetPerformanceCounters();
            _stringHashSetPool.ResetPerformanceCounters();
            _objectQueuePool.ResetPerformanceCounters();
            
            // Reset generic pools
            foreach (var pool in _genericListPools.Values) {
                if (pool is ObjectPool<List<object>> listPool) {
                    listPool.ResetPerformanceCounters();
                }
            }
            
            foreach (var pool in _genericDictionaryPools.Values) {
                if (pool is ObjectPool<Dictionary<string, object>> dictPool) {
                    dictPool.ResetPerformanceCounters();
                }
            }
        }

        /// <summary>
        /// Gets the total number of objects across all collection pools.
        /// </summary>
        public static int TotalPoolSize {
            get {
                var total = _objectListPool.PoolSize + 
                           _stringListPool.PoolSize + 
                           _stringObjectDictionaryPool.PoolSize + 
                           _stringStringDictionaryPool.PoolSize + 
                           _stringHashSetPool.PoolSize + 
                           _objectQueuePool.PoolSize;
                
                // Add generic pools
                foreach (var pool in _genericListPools.Values) {
                    if (pool is ObjectPool<List<object>> listPool) {
                        total += listPool.PoolSize;
                    }
                }
                
                foreach (var pool in _genericDictionaryPools.Values) {
                    if (pool is ObjectPool<Dictionary<string, object>> dictPool) {
                        total += dictPool.PoolSize;
                    }
                }
                
                return total;
            }
        }

        /// <summary>
        /// Gets the combined hit rate across all collection pools.
        /// </summary>
        public static double AverageHitRate {
            get {
                var pools = new ObjectPool<object>[] { 
                    (ObjectPool<object>)(object)_objectListPool, 
                    (ObjectPool<object>)(object)_stringListPool, 
                    (ObjectPool<object>)(object)_stringObjectDictionaryPool, 
                    (ObjectPool<object>)(object)_stringStringDictionaryPool, 
                    (ObjectPool<object>)(object)_stringHashSetPool, 
                    (ObjectPool<object>)(object)_objectQueuePool 
                };
                
                double totalHitRate = 0;
                int poolCount = 0;

                foreach (var pool in pools) {
                    if (pool.HitRate > 0) {
                        totalHitRate += pool.HitRate;
                        poolCount++;
                    }
                }

                return poolCount > 0 ? totalHitRate / poolCount : 0;
            }
        }

        /// <summary>
        /// Clears all collection pools. Use with caution.
        /// </summary>
        public static void ClearAllPools() {
            _objectListPool.Clear();
            _stringListPool.Clear();
            _stringObjectDictionaryPool.Clear();
            _stringStringDictionaryPool.Clear();
            _stringHashSetPool.Clear();
            _objectQueuePool.Clear();
            
            // Clear generic pools
            _genericListPools.Clear();
            _genericDictionaryPools.Clear();
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for convenient usage of collection pools with automatic return to pool.
    /// </summary>
    public static class CollectionPoolExtensions {
        /// <summary>
        /// Gets a pooled List&lt;T&gt; wrapped in a PooledObject that automatically returns to pool when disposed.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="capacity">The initial capacity for the list.</param>
        /// <returns>A PooledObject containing the List&lt;T&gt;.</returns>
        public static PooledList<T> GetPooledList<T>(int capacity = -1) {
            return new PooledList<T>(CollectionPool.GetList<T>(capacity));
        }

        /// <summary>
        /// Gets a pooled Dictionary&lt;TKey, TValue&gt; wrapped in a PooledObject that automatically returns to pool when disposed.
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
        /// <param name="capacity">The initial capacity for the dictionary.</param>
        /// <returns>A PooledObject containing the Dictionary&lt;TKey, TValue&gt;.</returns>
        public static PooledDictionary<TKey, TValue> GetPooledDictionary<TKey, TValue>(int capacity = -1) {
            return new PooledDictionary<TKey, TValue>(CollectionPool.GetDictionary<TKey, TValue>(capacity));
        }
    }

    /// <summary>
    /// Wrapper for pooled List&lt;T&gt; that automatically returns to pool when disposed.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public struct PooledList<T> : IDisposable {
        private readonly List<T> _list;
        private bool _disposed;

        internal PooledList(List<T> list) {
            _list = list;
            _disposed = false;
        }

        public List<T> Value => _disposed ? throw new ObjectDisposedException(nameof(PooledList<T>)) : _list;

        public void Dispose() {
            if (!_disposed && _list != null) {
                CollectionPool.ReturnList(_list);
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Wrapper for pooled Dictionary&lt;TKey, TValue&gt; that automatically returns to pool when disposed.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    public struct PooledDictionary<TKey, TValue> : IDisposable {
        private readonly Dictionary<TKey, TValue> _dictionary;
        private bool _disposed;

        internal PooledDictionary(Dictionary<TKey, TValue> dictionary) {
            _dictionary = dictionary;
            _disposed = false;
        }

        public Dictionary<TKey, TValue> Value => _disposed ? throw new ObjectDisposedException(nameof(PooledDictionary<TKey, TValue>)) : _dictionary;

        public void Dispose() {
            if (!_disposed && _dictionary != null) {
                CollectionPool.ReturnDictionary(_dictionary);
                _disposed = true;
            }
        }
    }
}
