using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Blish_HUD.Input;

namespace Blish_HUD._Utils {
    /// <summary>
    /// Provides object pools for commonly used event argument types to reduce allocations
    /// and improve performance in event handling systems.
    /// Since event arguments are typically immutable, this pool uses a cache-based approach
    /// for common event argument combinations.
    /// </summary>
    public static class EventArgsPool {
        
        // Cache for common MouseEventArgs combinations
        private static readonly ConcurrentDictionary<(MouseEventType, bool), MouseEventArgs> _mouseEventArgsCache = 
            new ConcurrentDictionary<(MouseEventType, bool), MouseEventArgs>();

        // Cache for common KeyboardEventArgs combinations
        private static readonly ConcurrentDictionary<(KeyboardEventType, Keys), KeyboardEventArgs> _keyboardEventArgsCache = 
            new ConcurrentDictionary<(KeyboardEventType, Keys), KeyboardEventArgs>();

        // Performance tracking
        private static long _mouseEventArgsCacheHits = 0;
        private static long _mouseEventArgsCacheMisses = 0;
        private static long _keyboardEventArgsCacheHits = 0;
        private static long _keyboardEventArgsCacheMisses = 0;

        // Maximum cache sizes to prevent memory bloat
        private const int MAX_MOUSE_CACHE_SIZE = 200;
        private const int MAX_KEYBOARD_CACHE_SIZE = 100;

        // Cache for common property names to avoid repeated allocations
        private static readonly Dictionary<string, System.ComponentModel.PropertyChangedEventArgs> _commonPropertyChangedArgs = 
            new Dictionary<string, System.ComponentModel.PropertyChangedEventArgs> {
                { "Visible", new System.ComponentModel.PropertyChangedEventArgs("Visible") },
                { "Enabled", new System.ComponentModel.PropertyChangedEventArgs("Enabled") },
                { "Location", new System.ComponentModel.PropertyChangedEventArgs("Location") },
                { "Size", new System.ComponentModel.PropertyChangedEventArgs("Size") },
                { "Text", new System.ComponentModel.PropertyChangedEventArgs("Text") },
                { "Value", new System.ComponentModel.PropertyChangedEventArgs("Value") },
                { "Selected", new System.ComponentModel.PropertyChangedEventArgs("Selected") },
                { "Checked", new System.ComponentModel.PropertyChangedEventArgs("Checked") },
                { "Opacity", new System.ComponentModel.PropertyChangedEventArgs("Opacity") },
                { "BackgroundColor", new System.ComponentModel.PropertyChangedEventArgs("BackgroundColor") },
                { "ForegroundColor", new System.ComponentModel.PropertyChangedEventArgs("ForegroundColor") },
                { "Font", new System.ComponentModel.PropertyChangedEventArgs("Font") },
                { "Parent", new System.ComponentModel.PropertyChangedEventArgs("Parent") },
                { "ZIndex", new System.ComponentModel.PropertyChangedEventArgs("ZIndex") }
            };

        #region MouseEventArgs Cache

        /// <summary>
        /// Gets a cached MouseEventArgs instance for common event type combinations.
        /// Creates and caches new instances for uncommon combinations.
        /// </summary>
        /// <param name="eventType">The type of mouse event.</param>
        /// <param name="isDoubleClick">Whether the event is a double-click.</param>
        /// <returns>A MouseEventArgs instance.</returns>
        public static MouseEventArgs GetMouseEventArgs(MouseEventType eventType, bool isDoubleClick = false) {
            var key = (eventType, isDoubleClick);
            
            if (_mouseEventArgsCache.TryGetValue(key, out var cachedArgs)) {
                System.Threading.Interlocked.Increment(ref _mouseEventArgsCacheHits);
                return cachedArgs;
            }

            System.Threading.Interlocked.Increment(ref _mouseEventArgsCacheMisses);

            // Create new instance
            var newArgs = isDoubleClick ? new MouseEventArgs(eventType, isDoubleClick) : new MouseEventArgs(eventType);

            // Cache it if we haven't exceeded the maximum cache size
            if (_mouseEventArgsCache.Count < MAX_MOUSE_CACHE_SIZE) {
                _mouseEventArgsCache.TryAdd(key, newArgs);
            }

            return newArgs;
        }

        /// <summary>
        /// Gets a cached MouseEventArgs instance for the specified event type.
        /// This is a convenience method for the most common case (no double-click).
        /// </summary>
        /// <param name="eventType">The type of mouse event.</param>
        /// <returns>A MouseEventArgs instance.</returns>
        public static MouseEventArgs GetMouseEventArgs(MouseEventType eventType) {
            return GetMouseEventArgs(eventType, false);
        }

        #endregion

        #region KeyboardEventArgs Cache

        /// <summary>
        /// Gets a cached KeyboardEventArgs instance for common event type and key combinations.
        /// Creates and caches new instances for uncommon combinations.
        /// </summary>
        /// <param name="eventType">The type of keyboard event.</param>
        /// <param name="key">The key that triggered the event.</param>
        /// <returns>A KeyboardEventArgs instance.</returns>
        public static KeyboardEventArgs GetKeyboardEventArgs(KeyboardEventType eventType, Keys key) {
            var cacheKey = (eventType, key);
            
            if (_keyboardEventArgsCache.TryGetValue(cacheKey, out var cachedArgs)) {
                System.Threading.Interlocked.Increment(ref _keyboardEventArgsCacheHits);
                return cachedArgs;
            }

            System.Threading.Interlocked.Increment(ref _keyboardEventArgsCacheMisses);

            // Create new instance
            var newArgs = new KeyboardEventArgs(eventType, key);

            // Cache it if we haven't exceeded the maximum cache size
            if (_keyboardEventArgsCache.Count < MAX_KEYBOARD_CACHE_SIZE) {
                _keyboardEventArgsCache.TryAdd(cacheKey, newArgs);
            }

            return newArgs;
        }

        #endregion

        #region PropertyChangedEventArgs Cache

        /// <summary>
        /// Gets a PropertyChangedEventArgs for the specified property name.
        /// Uses cached instances for common property names to maximize efficiency.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        /// <returns>A PropertyChangedEventArgs instance.</returns>
        public static System.ComponentModel.PropertyChangedEventArgs GetPropertyChangedEventArgs(string propertyName) {
            if (string.IsNullOrEmpty(propertyName)) {
                return new System.ComponentModel.PropertyChangedEventArgs(propertyName);
            }

            // Return cached instance for common property names
            if (_commonPropertyChangedArgs.TryGetValue(propertyName, out var cachedArgs)) {
                return cachedArgs;
            }

            // For uncommon property names, create a new instance
            return new System.ComponentModel.PropertyChangedEventArgs(propertyName);
        }

        #endregion

        #region Cache Statistics and Management

        /// <summary>
        /// Gets performance statistics for all event argument caches.
        /// </summary>
        /// <returns>A formatted string containing performance statistics for all caches.</returns>
        public static string GetPerformanceStatistics() {
            var mouseHitRate = (_mouseEventArgsCacheHits + _mouseEventArgsCacheMisses) > 0 
                ? (_mouseEventArgsCacheHits * 100.0 / (_mouseEventArgsCacheHits + _mouseEventArgsCacheMisses)) 
                : 0;
            
            var keyboardHitRate = (_keyboardEventArgsCacheHits + _keyboardEventArgsCacheMisses) > 0 
                ? (_keyboardEventArgsCacheHits * 100.0 / (_keyboardEventArgsCacheHits + _keyboardEventArgsCacheMisses)) 
                : 0;

            return $"Event Args Cache Statistics:\n" +
                   $"  MouseEventArgs Cache - Size: {_mouseEventArgsCache.Count}/{MAX_MOUSE_CACHE_SIZE}, " +
                   $"Hit Rate: {mouseHitRate:F1}% ({_mouseEventArgsCacheHits} hits, {_mouseEventArgsCacheMisses} misses)\n" +
                   $"  KeyboardEventArgs Cache - Size: {_keyboardEventArgsCache.Count}/{MAX_KEYBOARD_CACHE_SIZE}, " +
                   $"Hit Rate: {keyboardHitRate:F1}% ({_keyboardEventArgsCacheHits} hits, {_keyboardEventArgsCacheMisses} misses)";
        }

        /// <summary>
        /// Resets performance counters for all event argument caches.
        /// </summary>
        public static void ResetPerformanceCounters() {
            System.Threading.Interlocked.Exchange(ref _mouseEventArgsCacheHits, 0);
            System.Threading.Interlocked.Exchange(ref _mouseEventArgsCacheMisses, 0);
            System.Threading.Interlocked.Exchange(ref _keyboardEventArgsCacheHits, 0);
            System.Threading.Interlocked.Exchange(ref _keyboardEventArgsCacheMisses, 0);
        }

        /// <summary>
        /// Gets the total number of cached objects across all event argument caches.
        /// </summary>
        public static int TotalCacheSize => _mouseEventArgsCache.Count + _keyboardEventArgsCache.Count;

        /// <summary>
        /// Gets the combined hit rate across all event argument caches.
        /// </summary>
        public static double AverageHitRate {
            get {
                var totalHits = _mouseEventArgsCacheHits + _keyboardEventArgsCacheHits;
                var totalRequests = totalHits + _mouseEventArgsCacheMisses + _keyboardEventArgsCacheMisses;
                
                return totalRequests > 0 ? (totalHits * 100.0 / totalRequests) : 0;
            }
        }

        /// <summary>
        /// Clears all event argument caches. Use with caution.
        /// </summary>
        public static void ClearAllCaches() {
            _mouseEventArgsCache.Clear();
            _keyboardEventArgsCache.Clear();
        }

        /// <summary>
        /// Pre-populates the caches with common event argument combinations to improve initial performance.
        /// This method should be called during application initialization.
        /// </summary>
        public static void PrePopulateCaches() {
            // Pre-populate common MouseEventArgs
            var commonMouseEvents = new[] {
                MouseEventType.LeftMouseButtonPressed,
                MouseEventType.LeftMouseButtonReleased,
                MouseEventType.RightMouseButtonPressed,
                MouseEventType.RightMouseButtonReleased,
                MouseEventType.MouseMoved,
                MouseEventType.MouseEntered,
                MouseEventType.MouseLeft,
                MouseEventType.MouseWheelScrolled
            };

            foreach (var eventType in commonMouseEvents) {
                GetMouseEventArgs(eventType, false);
                GetMouseEventArgs(eventType, true); // Also cache double-click versions
            }

            // Pre-populate common KeyboardEventArgs
            var commonKeys = new[] {
                Keys.Escape, Keys.Enter, Keys.Space, Keys.Tab, Keys.Back,
                Keys.Left, Keys.Right, Keys.Up, Keys.Down,
                Keys.A, Keys.B, Keys.C, Keys.D, Keys.E, Keys.F, Keys.G, Keys.H, Keys.I, Keys.J,
                Keys.K, Keys.L, Keys.M, Keys.N, Keys.O, Keys.P, Keys.Q, Keys.R, Keys.S, Keys.T,
                Keys.U, Keys.V, Keys.W, Keys.X, Keys.Y, Keys.Z,
                Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9,
                Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F6, Keys.F7, Keys.F8, Keys.F9, Keys.F10, Keys.F11, Keys.F12
            };

            var commonKeyboardEvents = new[] { KeyboardEventType.KeyDown, KeyboardEventType.KeyUp };

            foreach (var eventType in commonKeyboardEvents) {
                foreach (var key in commonKeys) {
                    GetKeyboardEventArgs(eventType, key);
                }
            }
        }

        #endregion
    }
}
