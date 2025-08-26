using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Blish_HUD.GameServices.Graphics {
    /// <summary>
    /// High-performance cache for UI scaling calculations to eliminate redundant per-frame computations.
    /// Provides significant performance improvements by caching frequently used scaling operations.
    /// </summary>
    public class ScalingCache {
        private readonly ConcurrentDictionary<ScalingKey, ScalingResult> _pointScaleCache;
        private readonly ConcurrentDictionary<RectangleScalingKey, Rectangle> _rectangleScaleCache;
        private readonly ConcurrentDictionary<AspectRatioKey, float> _aspectRatioCache;
        
        private volatile float _currentUIScaleMultiplier = 1.0f;
        private volatile bool _cacheValid = true;
        
        // Performance counters
        private long _cacheHits = 0;
        private long _cacheMisses = 0;
        private long _cacheInvalidations = 0;
        
        // Cache size limits to prevent memory bloat
        private const int MAX_POINT_CACHE_SIZE = 1000;
        private const int MAX_RECTANGLE_CACHE_SIZE = 500;
        private const int MAX_ASPECT_RATIO_CACHE_SIZE = 200;
        
        public ScalingCache() {
            _pointScaleCache = new ConcurrentDictionary<ScalingKey, ScalingResult>();
            _rectangleScaleCache = new ConcurrentDictionary<RectangleScalingKey, Rectangle>();
            _aspectRatioCache = new ConcurrentDictionary<AspectRatioKey, float>();
        }
        
        /// <summary>
        /// Gets cached scaling result for a point, calculating and caching if not present.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ScalingResult GetPointScaling(Point point, float scaleMultiplier) {
            if (!_cacheValid || Math.Abs(scaleMultiplier - _currentUIScaleMultiplier) > 0.001f) {
                InvalidateCache(scaleMultiplier);
            }
            
            var key = new ScalingKey(point, scaleMultiplier);
            
            if (_pointScaleCache.TryGetValue(key, out var cachedResult)) {
                System.Threading.Interlocked.Increment(ref _cacheHits);
                return cachedResult;
            }
            
            // Cache miss - calculate and store
            System.Threading.Interlocked.Increment(ref _cacheMisses);
            
            var result = new ScalingResult(
                new Point((int)(point.X * scaleMultiplier), (int)(point.Y * scaleMultiplier)),
                new Point((int)(point.X / scaleMultiplier), (int)(point.Y / scaleMultiplier))
            );
            
            // Prevent cache from growing too large
            if (_pointScaleCache.Count < MAX_POINT_CACHE_SIZE) {
                _pointScaleCache.TryAdd(key, result);
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets cached rectangle scaling result, calculating and caching if not present.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Rectangle GetRectangleScaling(Rectangle rectangle, float scale) {
            if (!_cacheValid) {
                return CalculateRectangleScaling(rectangle, scale);
            }
            
            var key = new RectangleScalingKey(rectangle, scale);
            
            if (_rectangleScaleCache.TryGetValue(key, out var cachedResult)) {
                System.Threading.Interlocked.Increment(ref _cacheHits);
                return cachedResult;
            }
            
            // Cache miss - calculate and store
            System.Threading.Interlocked.Increment(ref _cacheMisses);
            var result = CalculateRectangleScaling(rectangle, scale);
            
            // Prevent cache from growing too large
            if (_rectangleScaleCache.Count < MAX_RECTANGLE_CACHE_SIZE) {
                _rectangleScaleCache.TryAdd(key, result);
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets cached aspect ratio scale factor, calculating and caching if not present.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetAspectRatioScale(Point src, int maxWidth, int maxHeight, ScaleMode scaleMode, bool enlarge) {
            var key = new AspectRatioKey(src, maxWidth, maxHeight, scaleMode, enlarge);
            
            if (_aspectRatioCache.TryGetValue(key, out var cachedResult)) {
                System.Threading.Interlocked.Increment(ref _cacheHits);
                return cachedResult;
            }
            
            // Cache miss - calculate and store
            System.Threading.Interlocked.Increment(ref _cacheMisses);
            var result = CalculateAspectRatioScale(src, maxWidth, maxHeight, scaleMode, enlarge);
            
            // Prevent cache from growing too large
            if (_aspectRatioCache.Count < MAX_ASPECT_RATIO_CACHE_SIZE) {
                _aspectRatioCache.TryAdd(key, result);
            }
            
            return result;
        }
        
        /// <summary>
        /// Invalidates the cache when the UI scale multiplier changes.
        /// </summary>
        public void InvalidateCache(float newUIScaleMultiplier) {
            if (Math.Abs(newUIScaleMultiplier - _currentUIScaleMultiplier) > 0.001f) {
                _currentUIScaleMultiplier = newUIScaleMultiplier;
                _cacheValid = false;
                
                // Clear all caches
                _pointScaleCache.Clear();
                _rectangleScaleCache.Clear();
                // Note: Aspect ratio cache doesn't depend on UI scale multiplier, so we don't clear it
                
                _cacheValid = true;
                System.Threading.Interlocked.Increment(ref _cacheInvalidations);
            }
        }
        
        /// <summary>
        /// Manually clears all caches. Use sparingly as this defeats the purpose of caching.
        /// </summary>
        public void ClearAllCaches() {
            _pointScaleCache.Clear();
            _rectangleScaleCache.Clear();
            _aspectRatioCache.Clear();
            System.Threading.Interlocked.Increment(ref _cacheInvalidations);
        }
        
        /// <summary>
        /// Gets performance statistics for the scaling cache.
        /// </summary>
        public CacheStatistics GetStatistics() {
            var totalRequests = _cacheHits + _cacheMisses;
            var hitRate = totalRequests > 0 ? (double)_cacheHits / totalRequests : 0.0;
            
            return new CacheStatistics {
                CacheHits = _cacheHits,
                CacheMisses = _cacheMisses,
                CacheInvalidations = _cacheInvalidations,
                HitRate = hitRate,
                PointCacheSize = _pointScaleCache.Count,
                RectangleCacheSize = _rectangleScaleCache.Count,
                AspectRatioCacheSize = _aspectRatioCache.Count,
                TotalCacheSize = _pointScaleCache.Count + _rectangleScaleCache.Count + _aspectRatioCache.Count
            };
        }
        
        /// <summary>
        /// Resets performance counters.
        /// </summary>
        public void ResetStatistics() {
            System.Threading.Interlocked.Exchange(ref _cacheHits, 0);
            System.Threading.Interlocked.Exchange(ref _cacheMisses, 0);
            System.Threading.Interlocked.Exchange(ref _cacheInvalidations, 0);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Rectangle CalculateRectangleScaling(Rectangle rectangle, float scale) {
            return new Rectangle(
                (int)Math.Floor(rectangle.X * scale),
                (int)Math.Floor(rectangle.Y * scale),
                (int)Math.Ceiling(rectangle.Width * scale),
                (int)Math.Ceiling(rectangle.Height * scale)
            );
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float CalculateAspectRatioScale(Point src, int maxWidth, int maxHeight, ScaleMode scaleMode, bool enlarge) {
            maxWidth = enlarge ? maxWidth : Math.Min(maxWidth, src.X);
            maxHeight = enlarge ? maxHeight : Math.Min(maxHeight, src.Y);

            float xScale = maxWidth / (float)src.X;
            float yScale = maxHeight / (float)src.Y;

            return scaleMode switch {
                ScaleMode.Fit => Math.Min(xScale, yScale),
                ScaleMode.Fill => Math.Max(xScale, yScale),
                _ => throw new NotImplementedException($"Unknown scaleMode {scaleMode}")
            };
        }
    }
    
    /// <summary>
    /// Cache key for point scaling operations.
    /// </summary>
    public readonly struct ScalingKey : IEquatable<ScalingKey> {
        public readonly Point Point;
        public readonly float ScaleMultiplier;
        
        public ScalingKey(Point point, float scaleMultiplier) {
            Point = point;
            ScaleMultiplier = scaleMultiplier;
        }
        
        public bool Equals(ScalingKey other) {
            return Point.Equals(other.Point) && Math.Abs(ScaleMultiplier - other.ScaleMultiplier) < 0.001f;
        }
        
        public override bool Equals(object obj) {
            return obj is ScalingKey other && Equals(other);
        }
        
        public override int GetHashCode() {
            return HashCode.Combine(Point, ScaleMultiplier);
        }
    }
    
    /// <summary>
    /// Cache key for rectangle scaling operations.
    /// </summary>
    public readonly struct RectangleScalingKey : IEquatable<RectangleScalingKey> {
        public readonly Rectangle Rectangle;
        public readonly float Scale;
        
        public RectangleScalingKey(Rectangle rectangle, float scale) {
            Rectangle = rectangle;
            Scale = scale;
        }
        
        public bool Equals(RectangleScalingKey other) {
            return Rectangle.Equals(other.Rectangle) && Math.Abs(Scale - other.Scale) < 0.001f;
        }
        
        public override bool Equals(object obj) {
            return obj is RectangleScalingKey other && Equals(other);
        }
        
        public override int GetHashCode() {
            return HashCode.Combine(Rectangle, Scale);
        }
    }
    
    /// <summary>
    /// Cache key for aspect ratio scaling operations.
    /// </summary>
    public readonly struct AspectRatioKey : IEquatable<AspectRatioKey> {
        public readonly Point Source;
        public readonly int MaxWidth;
        public readonly int MaxHeight;
        public readonly ScaleMode ScaleMode;
        public readonly bool Enlarge;
        
        public AspectRatioKey(Point source, int maxWidth, int maxHeight, ScaleMode scaleMode, bool enlarge) {
            Source = source;
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
            ScaleMode = scaleMode;
            Enlarge = enlarge;
        }
        
        public bool Equals(AspectRatioKey other) {
            return Source.Equals(other.Source) && MaxWidth == other.MaxWidth && MaxHeight == other.MaxHeight && 
                   ScaleMode == other.ScaleMode && Enlarge == other.Enlarge;
        }
        
        public override bool Equals(object obj) {
            return obj is AspectRatioKey other && Equals(other);
        }
        
        public override int GetHashCode() {
            return HashCode.Combine(Source, MaxWidth, MaxHeight, ScaleMode, Enlarge);
        }
    }
    
    /// <summary>
    /// Result of point scaling operations, containing both scaled and de-scaled versions.
    /// </summary>
    public readonly struct ScalingResult {
        public readonly Point ScaledPoint;
        public readonly Point DeScaledPoint;
        
        public ScalingResult(Point scaledPoint, Point deScaledPoint) {
            ScaledPoint = scaledPoint;
            DeScaledPoint = deScaledPoint;
        }
    }
    
    /// <summary>
    /// Performance statistics for the scaling cache.
    /// </summary>
    public struct CacheStatistics {
        public long CacheHits;
        public long CacheMisses;
        public long CacheInvalidations;
        public double HitRate;
        public int PointCacheSize;
        public int RectangleCacheSize;
        public int AspectRatioCacheSize;
        public int TotalCacheSize;
        
        public override string ToString() {
            return $"Cache Stats - Hits: {CacheHits}, Misses: {CacheMisses}, Hit Rate: {HitRate:P2}, " +
                   $"Sizes: Points={PointCacheSize}, Rectangles={RectangleCacheSize}, AspectRatio={AspectRatioCacheSize}";
        }
    }
}
