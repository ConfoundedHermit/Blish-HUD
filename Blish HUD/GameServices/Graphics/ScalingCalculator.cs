using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Blish_HUD.GameServices.Graphics {
    /// <summary>
    /// Optimized scaling calculator that uses caching and efficient mathematical operations
    /// to improve UI scaling performance. Provides significant performance improvements
    /// over direct calculations by eliminating redundant computations.
    /// </summary>
    public class ScalingCalculator {
        private readonly ScalingCache _cache;
        private static readonly Logger Logger = Logger.GetLogger<ScalingCalculator>();
        
        // Pre-calculated constants for common scaling operations
        private const float SCALE_EPSILON = 0.001f;
        
        public ScalingCalculator() {
            _cache = new ScalingCache();
        }
        
        /// <summary>
        /// Gets the scaling cache instance for direct access to cache statistics.
        /// </summary>
        public ScalingCache Cache => _cache;
        
        /// <summary>
        /// Scales a point to UI coordinates using cached calculations.
        /// Optimized version of the original ScaleToUi extension method.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point ScaleToUi(Point point, float uiScaleMultiplier) {
            if (Math.Abs(uiScaleMultiplier - 1.0f) < SCALE_EPSILON) {
                return point; // No scaling needed
            }
            
            var result = _cache.GetPointScaling(point, uiScaleMultiplier);
            return result.ScaledPoint;
        }
        
        /// <summary>
        /// De-scales a point from UI coordinates using cached calculations.
        /// Optimized version of the original UiToScale extension method.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point UiToScale(Point point, float uiScaleMultiplier) {
            if (Math.Abs(uiScaleMultiplier - 1.0f) < SCALE_EPSILON) {
                return point; // No scaling needed
            }
            
            var result = _cache.GetPointScaling(point, uiScaleMultiplier);
            return result.DeScaledPoint;
        }
        
        /// <summary>
        /// Scales a rectangle using cached calculations.
        /// Optimized version of the original ScaleBy extension method.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Rectangle ScaleRectangle(Rectangle rectangle, float scale) {
            if (Math.Abs(scale - 1.0f) < SCALE_EPSILON) {
                return rectangle; // No scaling needed
            }
            
            return _cache.GetRectangleScaling(rectangle, scale);
        }
        
        /// <summary>
        /// Calculates aspect ratio scale using cached calculations.
        /// Optimized version of the original GetAspectRatioScale extension method.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetAspectRatioScale(Point src, int maxWidth, int maxHeight, ScaleMode scaleMode = ScaleMode.Fit, bool enlarge = false) {
            return _cache.GetAspectRatioScale(src, maxWidth, maxHeight, scaleMode, enlarge);
        }
        
        /// <summary>
        /// Calculates aspect ratio scale using Point parameters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetAspectRatioScale(Point src, Point max, ScaleMode scaleMode = ScaleMode.Fit, bool enlarge = false) {
            return GetAspectRatioScale(src, max.X, max.Y, scaleMode, enlarge);
        }
        
        /// <summary>
        /// Resizes a point while keeping aspect ratio using cached calculations.
        /// Optimized version of the original ResizeKeepAspect extension method.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point ResizeKeepAspect(Point src, int maxWidth, int maxHeight, ScaleMode scaleMode = ScaleMode.Fit, bool enlarge = false) {
            float scale = GetAspectRatioScale(src, maxWidth, maxHeight, scaleMode, enlarge);
            return new Point((int)Math.Round(src.X * scale), (int)Math.Round(src.Y * scale));
        }
        
        /// <summary>
        /// Resizes a point while keeping aspect ratio using Point parameters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point ResizeKeepAspect(Point src, Point max, ScaleMode scaleMode = ScaleMode.Fit, bool enlarge = false) {
            return ResizeKeepAspect(src, max.X, max.Y, scaleMode, enlarge);
        }
        
        /// <summary>
        /// Batch scales multiple points efficiently. Useful for scaling many points at once
        /// with the same scale multiplier, reducing cache lookup overhead.
        /// </summary>
        public Point[] BatchScaleToUi(Point[] points, float uiScaleMultiplier) {
            if (points == null || points.Length == 0) return points;
            
            if (Math.Abs(uiScaleMultiplier - 1.0f) < SCALE_EPSILON) {
                return points; // No scaling needed, return original array
            }
            
            var results = new Point[points.Length];
            for (int i = 0; i < points.Length; i++) {
                var result = _cache.GetPointScaling(points[i], uiScaleMultiplier);
                results[i] = result.ScaledPoint;
            }
            
            return results;
        }
        
        /// <summary>
        /// Batch de-scales multiple points efficiently.
        /// </summary>
        public Point[] BatchUiToScale(Point[] points, float uiScaleMultiplier) {
            if (points == null || points.Length == 0) return points;
            
            if (Math.Abs(uiScaleMultiplier - 1.0f) < SCALE_EPSILON) {
                return points; // No scaling needed, return original array
            }
            
            var results = new Point[points.Length];
            for (int i = 0; i < points.Length; i++) {
                var result = _cache.GetPointScaling(points[i], uiScaleMultiplier);
                results[i] = result.DeScaledPoint;
            }
            
            return results;
        }
        
        /// <summary>
        /// Batch scales multiple rectangles efficiently.
        /// </summary>
        public Rectangle[] BatchScaleRectangles(Rectangle[] rectangles, float scale) {
            if (rectangles == null || rectangles.Length == 0) return rectangles;
            
            if (Math.Abs(scale - 1.0f) < SCALE_EPSILON) {
                return rectangles; // No scaling needed, return original array
            }
            
            var results = new Rectangle[rectangles.Length];
            for (int i = 0; i < rectangles.Length; i++) {
                results[i] = _cache.GetRectangleScaling(rectangles[i], scale);
            }
            
            return results;
        }
        
        /// <summary>
        /// Invalidates the scaling cache when UI scale changes.
        /// Should be called whenever the UI scale multiplier changes.
        /// </summary>
        public void InvalidateCache(float newUIScaleMultiplier) {
            _cache.InvalidateCache(newUIScaleMultiplier);
            Logger.Debug($"Scaling cache invalidated for new UI scale multiplier: {newUIScaleMultiplier}");
        }
        
        /// <summary>
        /// Gets comprehensive performance statistics from the scaling calculator.
        /// </summary>
        public string GetPerformanceStatistics() {
            var stats = _cache.GetStatistics();
            return $"Scaling Calculator Performance:\n{stats}\n" +
                   $"Cache Efficiency: {(stats.HitRate > 0.8 ? "EXCELLENT" : stats.HitRate > 0.6 ? "GOOD" : "NEEDS IMPROVEMENT")}";
        }
        
        /// <summary>
        /// Resets all performance counters.
        /// </summary>
        public void ResetPerformanceCounters() {
            _cache.ResetStatistics();
            Logger.Info("Scaling calculator performance counters have been reset.");
        }
        
        /// <summary>
        /// Performs a cache warmup by pre-calculating common scaling operations.
        /// This can improve performance for the first few frames after startup.
        /// </summary>
        public void WarmupCache(float uiScaleMultiplier) {
            Logger.Debug("Warming up scaling cache with common operations...");
            
            // Common UI element sizes
            var commonSizes = new[] {
                new Point(16, 16),   // Small icons
                new Point(32, 32),   // Medium icons
                new Point(64, 64),   // Large icons
                new Point(128, 128), // Very large icons
                new Point(256, 256), // Textures
                new Point(100, 20),  // Buttons
                new Point(200, 30),  // Text fields
                new Point(300, 200), // Panels
                new Point(400, 300), // Windows
                new Point(800, 600), // Large windows
            };
            
            // Pre-calculate scaling for common sizes
            foreach (var size in commonSizes) {
                _cache.GetPointScaling(size, uiScaleMultiplier);
                
                // Also pre-calculate some common rectangles
                var rect = new Rectangle(0, 0, size.X, size.Y);
                _cache.GetRectangleScaling(rect, uiScaleMultiplier);
                
                // And some common aspect ratio calculations
                _cache.GetAspectRatioScale(size, 1920, 1080, ScaleMode.Fit, false);
                _cache.GetAspectRatioScale(size, 1920, 1080, ScaleMode.Fill, false);
            }
            
            Logger.Debug($"Cache warmup complete. Pre-calculated {commonSizes.Length * 4} common operations.");
        }
    }
}
