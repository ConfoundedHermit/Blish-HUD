using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Blish_HUD._Utils {
    /// <summary>
    /// Provides a thread-safe string pool for commonly used strings to reduce memory allocations
    /// and improve string comparison performance through reference equality.
    /// </summary>
    public static class StringPool {
        private static readonly ConcurrentDictionary<string, string> _pool = new ConcurrentDictionary<string, string>();
        private static readonly int _maxPoolSize = 1000;
        private static int _currentCount = 0;

        // Performance tracking
        private static long _totalGets = 0;
        private static long _poolHits = 0;
        private static long _poolMisses = 0;

        /// <summary>
        /// Gets a pooled string instance. If the string is already in the pool, returns the pooled instance.
        /// Otherwise, adds the string to the pool and returns it.
        /// </summary>
        /// <param name="value">The string to pool.</param>
        /// <returns>A pooled string instance.</returns>
        public static string GetPooled(string value) {
            if (string.IsNullOrEmpty(value)) return value;

            Interlocked.Increment(ref _totalGets);

            if (_pool.TryGetValue(value, out string pooledValue)) {
                Interlocked.Increment(ref _poolHits);
                return pooledValue;
            }

            Interlocked.Increment(ref _poolMisses);

            // Only add to pool if we haven't exceeded the maximum pool size
            if (_currentCount < _maxPoolSize) {
                var internedValue = string.Intern(value);
                if (_pool.TryAdd(value, internedValue)) {
                    Interlocked.Increment(ref _currentCount);
                }
                return internedValue;
            }

            // If pool is full, just return the original string
            return value;
        }

        /// <summary>
        /// Gets performance statistics for the string pool.
        /// </summary>
        /// <returns>A formatted string containing pool performance statistics.</returns>
        public static string GetPerformanceStatistics() {
            var hitRate = _totalGets > 0 ? (_poolHits * 100.0 / _totalGets) : 0;
            return $"String Pool Stats - Pool Size: {_currentCount}/{_maxPoolSize}, " +
                   $"Total Gets: {_totalGets}, " +
                   $"Hit Rate: {hitRate:F1}% ({_poolHits} hits, {_poolMisses} misses)";
        }

        /// <summary>
        /// Resets the performance counters for the string pool.
        /// </summary>
        public static void ResetPerformanceCounters() {
            Interlocked.Exchange(ref _totalGets, 0);
            Interlocked.Exchange(ref _poolHits, 0);
            Interlocked.Exchange(ref _poolMisses, 0);
        }

        /// <summary>
        /// Gets the current number of strings in the pool.
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

        /// <summary>
        /// Clears the string pool. Use with caution as this may affect performance.
        /// </summary>
        public static void Clear() {
            _pool.Clear();
            Interlocked.Exchange(ref _currentCount, 0);
        }
    }

    /// <summary>
    /// Provides specialized string pools for common categories of strings.
    /// </summary>
    public static class SpecializedStringPools {
        
        /// <summary>
        /// String pool for file extensions commonly used in Blish HUD.
        /// </summary>
        public static class FileExtensions {
            private static readonly Dictionary<string, string> _commonExtensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                { ".bhm", string.Intern(".bhm") },
                { ".json", string.Intern(".json") },
                { ".wav", string.Intern(".wav") },
                { ".dll", string.Intern(".dll") },
                { ".exe", string.Intern(".exe") },
                { ".xml", string.Intern(".xml") },
                { ".gz", string.Intern(".gz") },
                { ".zip", string.Intern(".zip") },
                { ".dat", string.Intern(".dat") },
                { ".png", string.Intern(".png") },
                { ".jpg", string.Intern(".jpg") },
                { ".jpeg", string.Intern(".jpeg") },
                { ".bmp", string.Intern(".bmp") },
                { ".tga", string.Intern(".tga") },
                { ".dds", string.Intern(".dds") }
            };

            /// <summary>
            /// Gets a pooled file extension string.
            /// </summary>
            /// <param name="extension">The file extension to pool.</param>
            /// <returns>A pooled file extension string.</returns>
            public static string GetPooled(string extension) {
                if (string.IsNullOrEmpty(extension)) return extension;
                
                return _commonExtensions.TryGetValue(extension, out string pooledExtension) 
                    ? pooledExtension 
                    : StringPool.GetPooled(extension);
            }
        }

        /// <summary>
        /// String pool for configuration and setting keys commonly used in Blish HUD.
        /// </summary>
        public static class ConfigurationKeys {
            private static readonly Dictionary<string, string> _commonKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                // Settings categories
                { "OverlayConfiguration", string.Intern("OverlayConfiguration") },
                { "DynamicHUDConfiguration", string.Intern("DynamicHUDConfiguration") },
                { "ModuleConfiguration", string.Intern("ModuleConfiguration") },
                { "ModuleRepoConfiguration", string.Intern("ModuleRepoConfiguration") },
                { "GraphicsConfiguration", string.Intern("GraphicsConfiguration") },
                { "DebugConfiguration", string.Intern("DebugConfiguration") },
                { "GameIntegrationConfiguration", string.Intern("GameIntegrationConfiguration") },
                { "Gw2WebApiConfiguration", string.Intern("Gw2WebApiConfiguration") },
                { "WindowSettings", string.Intern("WindowSettings") },
                { "WindowSettings2", string.Intern("WindowSettings2") },
                
                // Common setting keys
                { "Enabled", string.Intern("Enabled") },
                { "Volume", string.Intern("Volume") },
                { "GameVolume", string.Intern("GameVolume") },
                { "MuteIfNoGameAudio", string.Intern("MuteIfNoGameAudio") },
                { "OutputDevice", string.Intern("OutputDevice") },
                { "ModuleStates", string.Intern("ModuleStates") },
                { "ExportedOn", string.Intern("ExportedOn") },
                { "DefaultPkgsUrl", string.Intern("DefaultPkgsUrl") },
                { "AcknowledgedUpdates", string.Intern("AcknowledgedUpdates") },
                { "ApiKeyRepository", string.Intern("ApiKeyRepository") },
                
                // JSON keys
                { "T", string.Intern("T") },
                { "Key", string.Intern("Key") },
                { "Value", string.Intern("Value") },
                { "Lazy", string.Intern("Lazy") },
                { "Ui", string.Intern("Ui") },
                { "Entries", string.Intern("Entries") },
                { "Name", string.Intern("Name") },
                { "Namespace", string.Intern("Namespace") },
                { "Version", string.Intern("Version") }
            };

            /// <summary>
            /// Gets a pooled configuration key string.
            /// </summary>
            /// <param name="key">The configuration key to pool.</param>
            /// <returns>A pooled configuration key string.</returns>
            public static string GetPooled(string key) {
                if (string.IsNullOrEmpty(key)) return key;
                
                return _commonKeys.TryGetValue(key, out string pooledKey) 
                    ? pooledKey 
                    : StringPool.GetPooled(key);
            }
        }

        /// <summary>
        /// String pool for common UI and graphics-related strings.
        /// </summary>
        public static class UIStrings {
            private static readonly Dictionary<string, string> _commonStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                // Common UI states
                { "button", string.Intern("button") },
                { "No Title", string.Intern("No Title") },
                { "-unchecked", string.Intern("-unchecked") },
                { "-checked", string.Intern("-checked") },
                { "None", string.Intern("None") },
                { "Unknown", string.Intern("Unknown") },
                
                // Graphics settings values (additional to GfxSettings)
                { "true", string.Intern("true") },
                { "false", string.Intern("false") },
                { "enabled", string.Intern("enabled") },
                { "disabled", string.Intern("disabled") },
                
                // Common texture and resource names
                { "ref", string.Intern("ref") },
                { "Mask", string.Intern("Mask") },
                { "Overlay", string.Intern("Overlay") },
                { "Roller", string.Intern("Roller") },
                { "Opacity", string.Intern("Opacity") }
            };

            /// <summary>
            /// Gets a pooled UI string.
            /// </summary>
            /// <param name="uiString">The UI string to pool.</param>
            /// <returns>A pooled UI string.</returns>
            public static string GetPooled(string uiString) {
                if (string.IsNullOrEmpty(uiString)) return uiString;
                
                return _commonStrings.TryGetValue(uiString, out string pooledString) 
                    ? pooledString 
                    : StringPool.GetPooled(uiString);
            }
        }

        /// <summary>
        /// String pool for module-related identifiers and paths.
        /// </summary>
        public static class ModuleStrings {
            private static readonly Dictionary<string, string> _commonStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                // Module-related constants
                { "modules", string.Intern("modules") },
                { "manifest.json", string.Intern("manifest.json") },
                { "compatibility.json", string.Intern("compatibility.json") },
                { "bh.blishhud", string.Intern("bh.blishhud") },
                { "/packages.gz", string.Intern("/packages.gz") },
                { "/preview-packages.gz", string.Intern("/preview-packages.gz") },
                
                // Common module operations
                { "NotDetected", string.Intern("NotDetected") },
                { "0.0.0", string.Intern("0.0.0") }
            };

            /// <summary>
            /// Gets a pooled module string.
            /// </summary>
            /// <param name="moduleString">The module string to pool.</param>
            /// <returns>A pooled module string.</returns>
            public static string GetPooled(string moduleString) {
                if (string.IsNullOrEmpty(moduleString)) return moduleString;
                
                return _commonStrings.TryGetValue(moduleString, out string pooledString) 
                    ? pooledString 
                    : StringPool.GetPooled(moduleString);
            }
        }
    }
}
