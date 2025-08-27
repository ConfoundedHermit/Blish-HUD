using System;
using System.Text;
using System.Threading;

namespace Blish_HUD._Utils {
    /// <summary>
    /// Provides centralized monitoring and statistics for all object pools in the application.
    /// This class aggregates performance data from all pool implementations to provide
    /// comprehensive insights into memory allocation optimization effectiveness.
    /// </summary>
    public static class PoolMonitor {
        
        // Performance tracking for overall pool system
        private static long _totalPoolRequests = 0;
        private static long _totalPoolHits = 0;
        private static long _totalPoolMisses = 0;
        private static DateTime _monitoringStartTime = DateTime.UtcNow;
        
        // Monitoring state
        private static bool _isMonitoringEnabled = true;
        private static Timer _periodicStatsTimer;
        
        static PoolMonitor() {
            // Initialize periodic statistics collection (every 5 minutes)
            _periodicStatsTimer = new Timer(CollectPeriodicStats, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        #region Public API

        /// <summary>
        /// Gets comprehensive performance statistics for all object pools in the system.
        /// </summary>
        /// <returns>A detailed report of all pool performance metrics.</returns>
        public static string GetComprehensiveStatistics() {
            var stats = new StringBuilder();
            var uptime = DateTime.UtcNow - _monitoringStartTime;
            
            stats.AppendLine("=== BLISH HUD OBJECT POOL MONITOR ===");
            stats.AppendLine($"Monitoring Uptime: {uptime:dd\\.hh\\:mm\\:ss}");
            stats.AppendLine($"Monitoring Status: {(_isMonitoringEnabled ? "ENABLED" : "DISABLED")}");
            stats.AppendLine($"Statistics Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            stats.AppendLine();
            
            // Overall system statistics
            var totalRequests = _totalPoolRequests;
            var overallHitRate = totalRequests > 0 ? (_totalPoolHits * 100.0 / totalRequests) : 0;
            
            stats.AppendLine("=== OVERALL POOL SYSTEM PERFORMANCE ===");
            stats.AppendLine($"Total Pool Requests: {totalRequests:N0}");
            stats.AppendLine($"Total Pool Hits: {_totalPoolHits:N0}");
            stats.AppendLine($"Total Pool Misses: {_totalPoolMisses:N0}");
            stats.AppendLine($"Overall Hit Rate: {overallHitRate:F2}%");
            stats.AppendLine($"Requests per Second: {(totalRequests / Math.Max(uptime.TotalSeconds, 1)):F2}");
            stats.AppendLine();
            
            // Individual pool statistics
            try {
                stats.AppendLine("=== INDIVIDUAL POOL STATISTICS ===");
                stats.AppendLine();
                
                // StringBuilder Pool
                stats.AppendLine("--- StringBuilder Pool ---");
                stats.AppendLine(StringBuilderPool.GetPerformanceStatistics());
                stats.AppendLine();
                
                // String Pool
                stats.AppendLine("--- String Pool ---");
                stats.AppendLine(StringPool.GetPerformanceStatistics());
                stats.AppendLine();
                
                // Event Args Pool
                stats.AppendLine("--- Event Arguments Pool ---");
                stats.AppendLine(EventArgsPool.GetPerformanceStatistics());
                stats.AppendLine();
                
                // Collection Pool
                stats.AppendLine("--- Collection Pool ---");
                stats.AppendLine(CollectionPool.GetPerformanceStatistics());
                stats.AppendLine();
                
                // Geometry Pool
                stats.AppendLine("--- Geometry Pool ---");
                stats.AppendLine(GeometryPool.GetPerformanceStatistics());
                stats.AppendLine();
                
                // Stream Pool
                stats.AppendLine("--- Stream Pool ---");
                stats.AppendLine(StreamPool.GetPerformanceStatistics());
                stats.AppendLine();
                
            } catch (Exception ex) {
                stats.AppendLine($"Error collecting individual pool statistics: {ex.Message}");
            }
            
            // Memory efficiency analysis
            stats.AppendLine("=== MEMORY EFFICIENCY ANALYSIS ===");
            var estimatedAllocationsAvoided = _totalPoolHits;
            var estimatedMemorySaved = estimatedAllocationsAvoided * 64; // Rough estimate: 64 bytes per avoided allocation
            
            stats.AppendLine($"Estimated Allocations Avoided: {estimatedAllocationsAvoided:N0}");
            stats.AppendLine($"Estimated Memory Saved: {estimatedMemorySaved:N0} bytes ({estimatedMemorySaved / 1024.0:F1} KB)");
            stats.AppendLine($"Estimated GC Pressure Reduction: {overallHitRate:F1}%");
            stats.AppendLine();
            
            // Performance recommendations
            stats.AppendLine("=== PERFORMANCE RECOMMENDATIONS ===");
            if (overallHitRate < 50) {
                stats.AppendLine("⚠️  LOW HIT RATE: Consider increasing pool sizes or reviewing usage patterns");
            } else if (overallHitRate > 90) {
                stats.AppendLine("✅ EXCELLENT HIT RATE: Pool configuration is optimal");
            } else {
                stats.AppendLine("✅ GOOD HIT RATE: Pool performance is satisfactory");
            }
            
            if (totalRequests / Math.Max(uptime.TotalHours, 1) > 10000) {
                stats.AppendLine("📈 HIGH USAGE: Pool system is heavily utilized - monitor for capacity issues");
            }
            
            stats.AppendLine();
            stats.AppendLine("=== END REPORT ===");
            
            return stats.ToString();
        }

        /// <summary>
        /// Gets a concise summary of pool performance suitable for logging or display.
        /// </summary>
        /// <returns>A brief summary of pool performance metrics.</returns>
        public static string GetSummaryStatistics() {
            var totalRequests = _totalPoolRequests;
            var hitRate = totalRequests > 0 ? (_totalPoolHits * 100.0 / totalRequests) : 0;
            var uptime = DateTime.UtcNow - _monitoringStartTime;
            
            return $"Pool Monitor Summary - Uptime: {uptime:hh\\:mm\\:ss}, " +
                   $"Requests: {totalRequests:N0}, Hit Rate: {hitRate:F1}%, " +
                   $"Memory Saved: {(_totalPoolHits * 64 / 1024.0):F1} KB";
        }

        /// <summary>
        /// Records a pool request for monitoring purposes.
        /// </summary>
        /// <param name="wasHit">True if the request was satisfied from the pool, false if a new object was created.</param>
        public static void RecordPoolRequest(bool wasHit) {
            if (!_isMonitoringEnabled) return;
            
            Interlocked.Increment(ref _totalPoolRequests);
            if (wasHit) {
                Interlocked.Increment(ref _totalPoolHits);
            } else {
                Interlocked.Increment(ref _totalPoolMisses);
            }
        }

        /// <summary>
        /// Resets all monitoring statistics and restarts the monitoring period.
        /// </summary>
        public static void ResetStatistics() {
            Interlocked.Exchange(ref _totalPoolRequests, 0);
            Interlocked.Exchange(ref _totalPoolHits, 0);
            Interlocked.Exchange(ref _totalPoolMisses, 0);
            _monitoringStartTime = DateTime.UtcNow;
            
            // Reset individual pool statistics
            try {
                StringBuilderPool.ResetPerformanceCounters();
                StringPool.ResetPerformanceCounters();
                EventArgsPool.ResetPerformanceCounters();
                CollectionPool.ResetPerformanceCounters();
                GeometryPool.ResetPerformanceCounters();
                StreamPool.ResetPerformanceCounters();
            } catch (Exception ex) {
                // Log error but don't throw - monitoring should be resilient
                System.Diagnostics.Debug.WriteLine($"Error resetting pool statistics: {ex.Message}");
            }
        }

        /// <summary>
        /// Enables or disables pool monitoring.
        /// </summary>
        /// <param name="enabled">True to enable monitoring, false to disable.</param>
        public static void SetMonitoringEnabled(bool enabled) {
            _isMonitoringEnabled = enabled;
        }

        /// <summary>
        /// Gets the current monitoring status.
        /// </summary>
        public static bool IsMonitoringEnabled => _isMonitoringEnabled;

        /// <summary>
        /// Gets the total number of pool requests since monitoring started.
        /// </summary>
        public static long TotalPoolRequests => _totalPoolRequests;

        /// <summary>
        /// Gets the total number of pool hits since monitoring started.
        /// </summary>
        public static long TotalPoolHits => _totalPoolHits;

        /// <summary>
        /// Gets the overall pool hit rate as a percentage.
        /// </summary>
        public static double OverallHitRate => _totalPoolRequests > 0 ? (_totalPoolHits * 100.0 / _totalPoolRequests) : 0;

        /// <summary>
        /// Gets the monitoring uptime.
        /// </summary>
        public static TimeSpan MonitoringUptime => DateTime.UtcNow - _monitoringStartTime;

        #endregion

        #region Private Methods

        /// <summary>
        /// Periodic callback to collect and optionally log pool statistics.
        /// </summary>
        private static void CollectPeriodicStats(object state) {
            if (!_isMonitoringEnabled) return;
            
            try {
                // This could be extended to log statistics to a file or send to a monitoring system
                var summary = GetSummaryStatistics();
                System.Diagnostics.Debug.WriteLine($"[PoolMonitor] {summary}");
                
                // Optional: Write to application log if logging is available
                // Logger.Info($"Pool Monitor: {summary}");
                
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"[PoolMonitor] Error in periodic stats collection: {ex.Message}");
            }
        }

        #endregion

        #region Diagnostic Methods

        /// <summary>
        /// Performs a diagnostic check of all pools and returns any issues found.
        /// </summary>
        /// <returns>A list of diagnostic messages, or empty string if no issues found.</returns>
        public static string RunDiagnostics() {
            var diagnostics = new StringBuilder();
            
            try {
                diagnostics.AppendLine("=== POOL SYSTEM DIAGNOSTICS ===");
                diagnostics.AppendLine($"Diagnostic Run Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                diagnostics.AppendLine();
                
                // Check overall system health
                var hitRate = OverallHitRate;
                if (hitRate < 30) {
                    diagnostics.AppendLine("❌ CRITICAL: Overall hit rate is very low - pool configuration may need adjustment");
                } else if (hitRate < 60) {
                    diagnostics.AppendLine("⚠️  WARNING: Overall hit rate is below optimal - consider reviewing pool sizes");
                } else {
                    diagnostics.AppendLine("✅ OK: Overall hit rate is acceptable");
                }
                
                // Check individual pools
                diagnostics.AppendLine();
                diagnostics.AppendLine("Individual Pool Health:");
                
                // StringBuilder Pool
                var sbHitRate = StringBuilderPool.HitRate;
                diagnostics.AppendLine($"  StringBuilder Pool: {sbHitRate:F1}% hit rate - {(sbHitRate > 70 ? "✅ OK" : "⚠️  LOW")}");
                
                // String Pool
                var stringHitRate = StringPool.HitRate;
                diagnostics.AppendLine($"  String Pool: {stringHitRate:F1}% hit rate - {(stringHitRate > 50 ? "✅ OK" : "⚠️  LOW")}");
                
                // Event Args Pool
                var eventHitRate = EventArgsPool.AverageHitRate;
                diagnostics.AppendLine($"  Event Args Pool: {eventHitRate:F1}% hit rate - {(eventHitRate > 60 ? "✅ OK" : "⚠️  LOW")}");
                
                // Collection Pool
                var collectionHitRate = CollectionPool.AverageHitRate;
                diagnostics.AppendLine($"  Collection Pool: {collectionHitRate:F1}% hit rate - {(collectionHitRate > 50 ? "✅ OK" : "⚠️  LOW")}");
                
                // Geometry Pool
                var geometryHitRate = GeometryPool.AverageHitRate;
                diagnostics.AppendLine($"  Geometry Pool: {geometryHitRate:F1}% hit rate - {(geometryHitRate > 60 ? "✅ OK" : "⚠️  LOW")}");
                
                // Stream Pool
                var streamHitRate = StreamPool.AverageHitRate;
                diagnostics.AppendLine($"  Stream Pool: {streamHitRate:F1}% hit rate - {(streamHitRate > 50 ? "✅ OK" : "⚠️  LOW")}");
                
                diagnostics.AppendLine();
                diagnostics.AppendLine("=== END DIAGNOSTICS ===");
                
            } catch (Exception ex) {
                diagnostics.AppendLine($"❌ ERROR: Failed to run diagnostics - {ex.Message}");
            }
            
            return diagnostics.ToString();
        }

        /// <summary>
        /// Exports detailed pool statistics to a formatted string suitable for file output.
        /// </summary>
        /// <returns>Formatted statistics suitable for export.</returns>
        public static string ExportStatistics() {
            var export = new StringBuilder();
            
            export.AppendLine("# Blish HUD Object Pool Statistics Export");
            export.AppendLine($"# Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            export.AppendLine($"# Monitoring Period: {MonitoringUptime:dd\\.hh\\:mm\\:ss}");
            export.AppendLine();
            
            export.AppendLine("## Summary");
            export.AppendLine($"- Total Requests: {_totalPoolRequests:N0}");
            export.AppendLine($"- Total Hits: {_totalPoolHits:N0}");
            export.AppendLine($"- Overall Hit Rate: {OverallHitRate:F2}%");
            export.AppendLine($"- Estimated Memory Saved: {(_totalPoolHits * 64 / 1024.0):F1} KB");
            export.AppendLine();
            
            export.AppendLine("## Detailed Statistics");
            export.AppendLine("```");
            export.AppendLine(GetComprehensiveStatistics());
            export.AppendLine("```");
            
            return export.ToString();
        }

        #endregion
    }
}
