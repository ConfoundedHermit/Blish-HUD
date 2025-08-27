using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Xna.Framework;

namespace Blish_HUD._Utils {
    /// <summary>
    /// Provides object pools for commonly used geometric wrapper types to reduce allocations
    /// and improve performance in UI layout, graphics operations, and positioning calculations.
    /// Since Point, Rectangle, and Vector2 are value types, this pool provides reference type wrappers
    /// that can be pooled and reused to avoid boxing allocations.
    /// </summary>
    public static class GeometryPool {
        
        // Object pools for geometric wrapper types (reference types that can be pooled)
        private static readonly ObjectPool<PointWrapper> _pointWrapperPool = new ObjectPool<PointWrapper>(
            objectGenerator: () => new PointWrapper(),
            resetAction: wrapper => wrapper.Reset(),
            validateAction: null,
            maxSize: 200
        );

        private static readonly ObjectPool<RectangleWrapper> _rectangleWrapperPool = new ObjectPool<RectangleWrapper>(
            objectGenerator: () => new RectangleWrapper(),
            resetAction: wrapper => wrapper.Reset(),
            validateAction: null,
            maxSize: 150
        );

        private static readonly ObjectPool<Vector2Wrapper> _vector2WrapperPool = new ObjectPool<Vector2Wrapper>(
            objectGenerator: () => new Vector2Wrapper(),
            resetAction: wrapper => wrapper.Reset(),
            validateAction: null,
            maxSize: 100
        );

        // Performance tracking
        private static long _pointRequests = 0;
        private static long _rectangleRequests = 0;
        private static long _vector2Requests = 0;

        #region Point Pool

        /// <summary>
        /// Gets a PointWrapper (reference type) from the pool.
        /// </summary>
        /// <returns>A PointWrapper instance ready for use.</returns>
        public static PointWrapper GetPointWrapper() {
            Interlocked.Increment(ref _pointRequests);
            return _pointWrapperPool.Get();
        }

        /// <summary>
        /// Gets a PointWrapper with the specified coordinates.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <returns>A PointWrapper with the specified coordinates.</returns>
        public static PointWrapper GetPointWrapper(int x, int y) {
            var wrapper = GetPointWrapper();
            wrapper.Set(x, y);
            return wrapper;
        }

        /// <summary>
        /// Returns a PointWrapper to the pool for reuse.
        /// </summary>
        /// <param name="pointWrapper">The PointWrapper to return to the pool.</param>
        public static void ReturnPointWrapper(PointWrapper pointWrapper) {
            if (pointWrapper != null) {
                _pointWrapperPool.Return(pointWrapper);
            }
        }

        #endregion

        #region Rectangle Pool

        /// <summary>
        /// Gets a RectangleWrapper (reference type) from the pool.
        /// </summary>
        /// <returns>A RectangleWrapper instance ready for use.</returns>
        public static RectangleWrapper GetRectangleWrapper() {
            Interlocked.Increment(ref _rectangleRequests);
            return _rectangleWrapperPool.Get();
        }

        /// <summary>
        /// Gets a RectangleWrapper with the specified bounds.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <returns>A RectangleWrapper with the specified bounds.</returns>
        public static RectangleWrapper GetRectangleWrapper(int x, int y, int width, int height) {
            var wrapper = GetRectangleWrapper();
            wrapper.Set(x, y, width, height);
            return wrapper;
        }

        /// <summary>
        /// Returns a RectangleWrapper to the pool for reuse.
        /// </summary>
        /// <param name="rectangleWrapper">The RectangleWrapper to return to the pool.</param>
        public static void ReturnRectangleWrapper(RectangleWrapper rectangleWrapper) {
            if (rectangleWrapper != null) {
                _rectangleWrapperPool.Return(rectangleWrapper);
            }
        }

        #endregion

        #region Vector2 Pool

        /// <summary>
        /// Gets a Vector2Wrapper (reference type) from the pool.
        /// </summary>
        /// <returns>A Vector2Wrapper instance ready for use.</returns>
        public static Vector2Wrapper GetVector2Wrapper() {
            Interlocked.Increment(ref _vector2Requests);
            return _vector2WrapperPool.Get();
        }

        /// <summary>
        /// Gets a Vector2Wrapper with the specified coordinates.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <returns>A Vector2Wrapper with the specified coordinates.</returns>
        public static Vector2Wrapper GetVector2Wrapper(float x, float y) {
            var wrapper = GetVector2Wrapper();
            wrapper.Set(x, y);
            return wrapper;
        }

        /// <summary>
        /// Returns a Vector2Wrapper to the pool for reuse.
        /// </summary>
        /// <param name="vector2Wrapper">The Vector2Wrapper to return to the pool.</param>
        public static void ReturnVector2Wrapper(Vector2Wrapper vector2Wrapper) {
            if (vector2Wrapper != null) {
                _vector2WrapperPool.Return(vector2Wrapper);
            }
        }

        #endregion

        #region Pool Statistics and Management

        /// <summary>
        /// Gets performance statistics for all geometry pools.
        /// </summary>
        /// <returns>A formatted string containing performance statistics for all geometry pools.</returns>
        public static string GetPerformanceStatistics() {
            var stats = "Geometry Pool Statistics:\n";
            stats += $"  Point Wrapper Pool - {_pointWrapperPool.GetPerformanceStatistics()}\n";
            stats += $"  Rectangle Wrapper Pool - {_rectangleWrapperPool.GetPerformanceStatistics()}\n";
            stats += $"  Vector2 Wrapper Pool - {_vector2WrapperPool.GetPerformanceStatistics()}\n";
            stats += $"  Total Geometry Requests: Point={_pointRequests}, Rectangle={_rectangleRequests}, Vector2={_vector2Requests}";
            
            return stats;
        }

        /// <summary>
        /// Resets performance counters for all geometry pools.
        /// </summary>
        public static void ResetPerformanceCounters() {
            _pointWrapperPool.ResetPerformanceCounters();
            _rectangleWrapperPool.ResetPerformanceCounters();
            _vector2WrapperPool.ResetPerformanceCounters();
            
            Interlocked.Exchange(ref _pointRequests, 0);
            Interlocked.Exchange(ref _rectangleRequests, 0);
            Interlocked.Exchange(ref _vector2Requests, 0);
        }

        /// <summary>
        /// Gets the total number of objects across all geometry pools.
        /// </summary>
        public static int TotalPoolSize => _pointWrapperPool.PoolSize + _rectangleWrapperPool.PoolSize + _vector2WrapperPool.PoolSize;

        /// <summary>
        /// Gets the combined hit rate across all geometry pools.
        /// </summary>
        public static double AverageHitRate {
            get {
                var pools = new ObjectPool<object>[] { 
                    (ObjectPool<object>)(object)_pointWrapperPool, 
                    (ObjectPool<object>)(object)_rectangleWrapperPool, 
                    (ObjectPool<object>)(object)_vector2WrapperPool 
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
        /// Clears all geometry pools. Use with caution.
        /// </summary>
        public static void ClearAllPools() {
            _pointWrapperPool.Clear();
            _rectangleWrapperPool.Clear();
            _vector2WrapperPool.Clear();
        }

        #endregion
    }

    #region Wrapper Classes for Reference Types

    /// <summary>
    /// Reference type wrapper for Point to enable object pooling.
    /// </summary>
    public class PointWrapper {
        public Point Value { get; private set; }

        public int X => Value.X;
        public int Y => Value.Y;

        public void Set(int x, int y) {
            Value = new Point(x, y);
        }

        public void Set(Point point) {
            Value = point;
        }

        public void Reset() {
            Value = Point.Zero;
        }

        public static implicit operator Point(PointWrapper wrapper) => wrapper.Value;

        public override string ToString() => Value.ToString();
    }

    /// <summary>
    /// Reference type wrapper for Rectangle to enable object pooling.
    /// </summary>
    public class RectangleWrapper {
        public Rectangle Value { get; private set; }

        public int X => Value.X;
        public int Y => Value.Y;
        public int Width => Value.Width;
        public int Height => Value.Height;
        public Point Location => Value.Location;
        public Point Size => new Point(Value.Width, Value.Height);

        public void Set(int x, int y, int width, int height) {
            Value = new Rectangle(x, y, width, height);
        }

        public void Set(Rectangle rectangle) {
            Value = rectangle;
        }

        public void Reset() {
            Value = Rectangle.Empty;
        }

        public static implicit operator Rectangle(RectangleWrapper wrapper) => wrapper.Value;

        public override string ToString() => Value.ToString();
    }

    /// <summary>
    /// Reference type wrapper for Vector2 to enable object pooling.
    /// </summary>
    public class Vector2Wrapper {
        public Vector2 Value { get; private set; }

        public float X => Value.X;
        public float Y => Value.Y;

        public void Set(float x, float y) {
            Value = new Vector2(x, y);
        }

        public void Set(Vector2 vector) {
            Value = vector;
        }

        public void Reset() {
            Value = Vector2.Zero;
        }

        public static implicit operator Vector2(Vector2Wrapper wrapper) => wrapper.Value;

        public override string ToString() => Value.ToString();
    }

    #endregion

    #region Extension Methods for Convenient Usage

    /// <summary>
    /// Extension methods for convenient usage of geometry pools with automatic return to pool.
    /// </summary>
    public static class GeometryPoolExtensions {
        /// <summary>
        /// Gets a pooled PointWrapper wrapped in a PooledObject that automatically returns to pool when disposed.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <returns>A PooledObject containing the PointWrapper.</returns>
        public static PooledPointWrapper GetPooledPoint(int x = 0, int y = 0) {
            return new PooledPointWrapper(GeometryPool.GetPointWrapper(x, y));
        }

        /// <summary>
        /// Gets a pooled RectangleWrapper wrapped in a PooledObject that automatically returns to pool when disposed.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <returns>A PooledObject containing the RectangleWrapper.</returns>
        public static PooledRectangleWrapper GetPooledRectangle(int x = 0, int y = 0, int width = 0, int height = 0) {
            return new PooledRectangleWrapper(GeometryPool.GetRectangleWrapper(x, y, width, height));
        }

        /// <summary>
        /// Gets a pooled Vector2Wrapper wrapped in a PooledObject that automatically returns to pool when disposed.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <returns>A PooledObject containing the Vector2Wrapper.</returns>
        public static PooledVector2Wrapper GetPooledVector2(float x = 0f, float y = 0f) {
            return new PooledVector2Wrapper(GeometryPool.GetVector2Wrapper(x, y));
        }
    }

    /// <summary>
    /// Wrapper for pooled PointWrapper that automatically returns to pool when disposed.
    /// </summary>
    public struct PooledPointWrapper : IDisposable {
        private readonly PointWrapper _pointWrapper;
        private bool _disposed;

        internal PooledPointWrapper(PointWrapper pointWrapper) {
            _pointWrapper = pointWrapper;
            _disposed = false;
        }

        public PointWrapper Value => _disposed ? throw new ObjectDisposedException(nameof(PooledPointWrapper)) : _pointWrapper;

        public void Dispose() {
            if (!_disposed && _pointWrapper != null) {
                GeometryPool.ReturnPointWrapper(_pointWrapper);
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Wrapper for pooled RectangleWrapper that automatically returns to pool when disposed.
    /// </summary>
    public struct PooledRectangleWrapper : IDisposable {
        private readonly RectangleWrapper _rectangleWrapper;
        private bool _disposed;

        internal PooledRectangleWrapper(RectangleWrapper rectangleWrapper) {
            _rectangleWrapper = rectangleWrapper;
            _disposed = false;
        }

        public RectangleWrapper Value => _disposed ? throw new ObjectDisposedException(nameof(PooledRectangleWrapper)) : _rectangleWrapper;

        public void Dispose() {
            if (!_disposed && _rectangleWrapper != null) {
                GeometryPool.ReturnRectangleWrapper(_rectangleWrapper);
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Wrapper for pooled Vector2Wrapper that automatically returns to pool when disposed.
    /// </summary>
    public struct PooledVector2Wrapper : IDisposable {
        private readonly Vector2Wrapper _vector2Wrapper;
        private bool _disposed;

        internal PooledVector2Wrapper(Vector2Wrapper vector2Wrapper) {
            _vector2Wrapper = vector2Wrapper;
            _disposed = false;
        }

        public Vector2Wrapper Value => _disposed ? throw new ObjectDisposedException(nameof(PooledVector2Wrapper)) : _vector2Wrapper;

        public void Dispose() {
            if (!_disposed && _vector2Wrapper != null) {
                GeometryPool.ReturnVector2Wrapper(_vector2Wrapper);
                _disposed = true;
            }
        }
    }

    #endregion
}
