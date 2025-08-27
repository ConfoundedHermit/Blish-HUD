using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Blish_HUD._Utils {
    /// <summary>
    /// Provides caching for format strings to reduce allocations in string formatting operations.
    /// This is particularly useful for logging and statistics generation where the same format
    /// strings are used repeatedly.
    /// </summary>
    public static class FormatStringCache {
        private static readonly ConcurrentDictionary<string, string> _formatCache = new ConcurrentDictionary<string, string>();
        private static readonly int _maxCacheSize = 500;
        private static int _currentCount = 0;

        // Performance tracking
        private static long _totalGets = 0;
        private static long _cacheHits = 0;
        private static long _cacheMisses = 0;

        /// <summary>
        /// Gets a cached and interned format string. If the format string is already cached,
        /// returns the cached instance. Otherwise, interns the string and adds it to the cache.
        /// </summary>
        /// <param name="format">The format string to cache.</param>
        /// <returns>A cached and interned format string.</returns>
        public static string GetCachedFormat(string format) {
            if (string.IsNullOrEmpty(format)) return format;

            Interlocked.Increment(ref _totalGets);

            if (_formatCache.TryGetValue(format, out string cachedFormat)) {
                Interlocked.Increment(ref _cacheHits);
                return cachedFormat;
            }

            Interlocked.Increment(ref _cacheMisses);

            // Only add to cache if we haven't exceeded the maximum cache size
            if (_currentCount < _maxCacheSize) {
                var internedFormat = string.Intern(format);
                if (_formatCache.TryAdd(format, internedFormat)) {
                    Interlocked.Increment(ref _currentCount);
                }
                return internedFormat;
            }

            // If cache is full, just return the original format string
            return format;
        }

        /// <summary>
        /// Gets performance statistics for the format string cache.
        /// </summary>
        /// <returns>A formatted string containing cache performance statistics.</returns>
        public static string GetPerformanceStatistics() {
            var hitRate = _totalGets > 0 ? (_cacheHits * 100.0 / _totalGets) : 0;
            return $"Format String Cache Stats - Cache Size: {_currentCount}/{_maxCacheSize}, " +
                   $"Total Gets: {_totalGets}, " +
                   $"Hit Rate: {hitRate:F1}% ({_cacheHits} hits, {_cacheMisses} misses)";
        }

        /// <summary>
        /// Resets the performance counters for the format string cache.
        /// </summary>
        public static void ResetPerformanceCounters() {
            Interlocked.Exchange(ref _totalGets, 0);
            Interlocked.Exchange(ref _cacheHits, 0);
            Interlocked.Exchange(ref _cacheMisses, 0);
        }

        /// <summary>
        /// Gets the current number of format strings in the cache.
        /// </summary>
        public static int CacheSize => _currentCount;

        /// <summary>
        /// Gets the maximum cache size.
        /// </summary>
        public static int MaxCacheSize => _maxCacheSize;

        /// <summary>
        /// Gets the cache hit rate as a percentage.
        /// </summary>
        public static double HitRate => _totalGets > 0 ? (_cacheHits * 100.0 / _totalGets) : 0;

        /// <summary>
        /// Clears the format string cache. Use with caution as this may affect performance.
        /// </summary>
        public static void Clear() {
            _formatCache.Clear();
            Interlocked.Exchange(ref _currentCount, 0);
        }
    }

    /// <summary>
    /// Provides optimized string formatting operations using cached format strings.
    /// </summary>
    public static class CachedStringFormatter {
        /// <summary>
        /// Formats a string using a cached format string and pooled StringBuilder.
        /// This combines format string caching with StringBuilder pooling for maximum efficiency.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        /// <returns>The formatted string.</returns>
        public static string Format(string format, params object[] args) {
            var cachedFormat = FormatStringCache.GetCachedFormat(format);
            var sb = StringBuilderPool.Get();
            try {
                sb.AppendFormat(cachedFormat, args);
                return sb.ToString();
            } finally {
                StringBuilderPool.Return(sb);
            }
        }

        /// <summary>
        /// Formats a string using a cached format string with a single argument.
        /// Optimized version for single-argument formatting.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        /// <returns>The formatted string.</returns>
        public static string Format(string format, object arg0) {
            var cachedFormat = FormatStringCache.GetCachedFormat(format);
            var sb = StringBuilderPool.Get();
            try {
                sb.AppendFormat(cachedFormat, arg0);
                return sb.ToString();
            } finally {
                StringBuilderPool.Return(sb);
            }
        }

        /// <summary>
        /// Formats a string using a cached format string with two arguments.
        /// Optimized version for two-argument formatting.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        /// <returns>The formatted string.</returns>
        public static string Format(string format, object arg0, object arg1) {
            var cachedFormat = FormatStringCache.GetCachedFormat(format);
            var sb = StringBuilderPool.Get();
            try {
                sb.AppendFormat(cachedFormat, arg0, arg1);
                return sb.ToString();
            } finally {
                StringBuilderPool.Return(sb);
            }
        }

        /// <summary>
        /// Formats a string using a cached format string with three arguments.
        /// Optimized version for three-argument formatting.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        /// <param name="arg2">The third argument for the format string.</param>
        /// <returns>The formatted string.</returns>
        public static string Format(string format, object arg0, object arg1, object arg2) {
            var cachedFormat = FormatStringCache.GetCachedFormat(format);
            var sb = StringBuilderPool.Get();
            try {
                sb.AppendFormat(cachedFormat, arg0, arg1, arg2);
                return sb.ToString();
            } finally {
                StringBuilderPool.Return(sb);
            }
        }
    }
}
