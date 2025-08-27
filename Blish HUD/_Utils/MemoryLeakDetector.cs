using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;

namespace Blish_HUD._Utils {

    /// <summary>
    /// Comprehensive memory leak detection and monitoring system.
    /// Provides runtime memory health checks, object reference tracking, and automated leak detection.
    /// </summary>
    public static class MemoryLeakDetector {
        
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(MemoryLeakDetector));
        
        // Memory monitoring
        private static readonly Timer _memoryMonitorTimer;
        private static long _lastGCMemory = 0;
        private static readonly List<MemorySnapshot> _memoryHistory = new List<MemorySnapshot>();
        private static readonly object _memoryHistoryLock = new object();
        
        // Object reference tracking
        private static readonly ConcurrentDictionary<string, ConcurrentBag<WeakReference>> _trackedObjects 
            = new ConcurrentDictionary<string, ConcurrentBag<WeakReference>>();
        
        // Memory health thresholds
        private static readonly MemoryHealthThresholds _thresholds = new MemoryHealthThresholds();
        
        // Statistics
        private static readonly MemoryLeakStatistics _statistics = new MemoryLeakStatistics();
        
        static MemoryLeakDetector() {
            // Start memory monitoring timer (every 30 seconds)
            _memoryMonitorTimer = new Timer(MonitorMemoryUsage, null, 
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            
            Logger.Info("Memory leak detector initialized");
        }

        /// <summary>
        /// Configures memory health monitoring thresholds.
        /// </summary>
        /// <param name="thresholds">Memory health thresholds</param>
        public static void ConfigureThresholds(MemoryHealthThresholds thresholds) {
            _thresholds.MaxMemoryGrowthMB = thresholds.MaxMemoryGrowthMB;
            _thresholds.MaxGCFrequencyPerMinute = thresholds.MaxGCFrequencyPerMinute;
            _thresholds.MaxWorkingSetMB = thresholds.MaxWorkingSetMB;
            _thresholds.MemoryHistorySize = thresholds.MemoryHistorySize;
            
            Logger.Info($"Memory thresholds configured: MaxGrowth={thresholds.MaxMemoryGrowthMB}MB, " +
                       $"MaxGC={thresholds.MaxGCFrequencyPerMinute}/min, MaxWorkingSet={thresholds.MaxWorkingSetMB}MB");
        }

        /// <summary>
        /// Tracks an object for memory leak detection.
        /// </summary>
        /// <param name="category">Category of the object (e.g., "Controls", "Services", "Modules")</param>
        /// <param name="obj">Object to track</param>
        /// <param name="description">Optional description for debugging</param>
        public static void TrackObject(string category, object obj, string description = null) {
            if (obj == null) return;
            
            var trackedObjects = _trackedObjects.GetOrAdd(category, _ => new ConcurrentBag<WeakReference>());
            var weakRef = new WeakReference(obj);
            
            // Store additional metadata if description provided
            if (!string.IsNullOrEmpty(description)) {
                weakRef.Target = new TrackedObjectWrapper {
                    Target = obj,
                    Description = description,
                    TrackingTime = DateTime.UtcNow
                };
            }
            
            trackedObjects.Add(weakRef);
            
            Interlocked.Increment(ref _statistics._totalObjectsTracked);
            
            #if DEBUG
            Logger.Debug($"Tracking object: {category} - {obj.GetType().Name} ({description})");
            #endif
        }

        /// <summary>
        /// Stops tracking an object (call when object is properly disposed).
        /// </summary>
        /// <param name="category">Category of the object</param>
        /// <param name="obj">Object to stop tracking</param>
        public static void UntrackObject(string category, object obj) {
            if (obj == null) return;
            
            if (_trackedObjects.TryGetValue(category, out var trackedObjects)) {
                // Note: ConcurrentBag doesn't support removal, but the weak reference will become null
                // and will be cleaned up during the next cleanup cycle
                Interlocked.Increment(ref _statistics._totalObjectsUntracked);
                
                #if DEBUG
                Logger.Debug($"Untracking object: {category} - {obj.GetType().Name}");
                #endif
            }
        }

        /// <summary>
        /// Performs a comprehensive memory health check.
        /// </summary>
        /// <returns>Memory health report</returns>
        public static MemoryHealthReport PerformHealthCheck() {
            var report = new MemoryHealthReport {
                CheckTime = DateTime.UtcNow,
                ProcessId = Process.GetCurrentProcess().Id
            };

            try {
                // Force garbage collection for accurate measurements
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // Get current memory metrics
                var process = Process.GetCurrentProcess();
                report.WorkingSetMB = process.WorkingSet64 / 1024 / 1024;
                report.PrivateMemoryMB = process.PrivateMemorySize64 / 1024 / 1024;
                report.ManagedMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024;
                
                // GC statistics
                report.Gen0Collections = GC.CollectionCount(0);
                report.Gen1Collections = GC.CollectionCount(1);
                report.Gen2Collections = GC.CollectionCount(2);
                
                // Memory pressure
                report.MemoryPressure = GetMemoryPressureLevel();
                
                // Check for memory growth
                CheckMemoryGrowth(report);
                
                // Check tracked objects for leaks
                CheckTrackedObjectLeaks(report);
                
                // Check event subscription leaks
                CheckEventSubscriptionLeaks(report);
                
                // Overall health assessment
                AssessOverallHealth(report);
                
                // Update statistics
                Interlocked.Increment(ref _statistics._totalHealthChecks);
                if (report.OverallHealth == MemoryHealthStatus.Critical) {
                    Interlocked.Increment(ref _statistics._criticalHealthChecks);
                }

                Logger.Info($"Memory health check completed: {report.OverallHealth} " +
                           $"(Working Set: {report.WorkingSetMB}MB, Managed: {report.ManagedMemoryMB}MB)");

            } catch (Exception ex) {
                Logger.Error(ex, "Error during memory health check");
                report.OverallHealth = MemoryHealthStatus.Error;
                report.Issues.Add($"Health check error: {ex.Message}");
            }

            return report;
        }

        /// <summary>
        /// Gets current memory leak detection statistics.
        /// </summary>
        /// <returns>Memory leak statistics</returns>
        public static MemoryLeakStatistics GetStatistics() {
            var stats = new MemoryLeakStatistics();
            stats._totalObjectsTracked = _statistics._totalObjectsTracked;
            stats._totalObjectsUntracked = _statistics._totalObjectsUntracked;
            stats._totalHealthChecks = _statistics._totalHealthChecks;
            stats._criticalHealthChecks = _statistics._criticalHealthChecks;
            stats._lastHealthCheck = _statistics._lastHealthCheck;

            // Calculate current tracked objects by category
            foreach (var kvp in _trackedObjects) {
                var category = kvp.Key;
                var objects = kvp.Value;
                
                var aliveCount = objects.Count(wr => wr.Target != null);
                var deadCount = objects.Count() - aliveCount;
                
                stats.TrackedObjectsByCategory[category] = new ObjectTrackingInfo {
                    AliveObjects = aliveCount,
                    DeadReferences = deadCount,
                    TotalTracked = objects.Count()
                };
            }

            return stats;
        }

        /// <summary>
        /// Cleans up dead references from tracked objects.
        /// </summary>
        public static void CleanupDeadReferences() {
            var cleanedCount = 0;
            
            foreach (var kvp in _trackedObjects) {
                var category = kvp.Key;
                var objects = kvp.Value;
                
                var aliveObjects = objects.Where(wr => wr.Target != null).ToArray();
                var deadCount = objects.Count() - aliveObjects.Length;
                
                if (deadCount > 0) {
                    // Replace the bag with only alive objects
                    var newBag = new ConcurrentBag<WeakReference>(aliveObjects);
                    _trackedObjects.TryUpdate(category, newBag, objects);
                    cleanedCount += deadCount;
                }
            }

            if (cleanedCount > 0) {
                Logger.Debug($"Cleaned up {cleanedCount} dead object references");
            }
        }

        /// <summary>
        /// Forces an immediate memory leak scan and report.
        /// </summary>
        /// <returns>List of detected potential leaks</returns>
        public static List<MemoryLeakInfo> ScanForLeaks() {
            var leaks = new List<MemoryLeakInfo>();
            
            try {
                // Scan tracked objects
                foreach (var kvp in _trackedObjects) {
                    var category = kvp.Key;
                    var objects = kvp.Value;
                    
                    var objectList = objects.ToArray();
                    var aliveObjects = objectList.Where(wr => wr.Target != null).ToArray();
                    
                    // Check for objects that have been alive too long
                    foreach (var weakRef in aliveObjects) {
                        if (weakRef.Target is TrackedObjectWrapper wrapper) {
                            var age = DateTime.UtcNow - wrapper.TrackingTime;
                            if (age.TotalMinutes > 30) { // Objects alive for more than 30 minutes
                                leaks.Add(new MemoryLeakInfo {
                                    Category = category,
                                    ObjectType = wrapper.Target?.GetType().Name ?? "Unknown",
                                    Description = wrapper.Description,
                                    Age = age,
                                    LeakType = MemoryLeakType.LongLivedObject
                                });
                            }
                        }
                    }
                }
                
                // Scan event subscriptions
                #if DEBUG
                EventLeakDetector.ReportLeaks();
                #endif
                
                Logger.Info($"Memory leak scan completed: {leaks.Count} potential leaks found");
                
            } catch (Exception ex) {
                Logger.Error(ex, "Error during memory leak scan");
            }
            
            return leaks;
        }

        private static void MonitorMemoryUsage(object state) {
            try {
                var currentMemory = GC.GetTotalMemory(false);
                var memoryGrowth = currentMemory - _lastGCMemory;
                
                // Create memory snapshot
                var snapshot = new MemorySnapshot {
                    Timestamp = DateTime.UtcNow,
                    ManagedMemory = currentMemory,
                    WorkingSet = Process.GetCurrentProcess().WorkingSet64,
                    Gen0Collections = GC.CollectionCount(0),
                    Gen1Collections = GC.CollectionCount(1),
                    Gen2Collections = GC.CollectionCount(2)
                };
                
                // Store snapshot
                lock (_memoryHistoryLock) {
                    _memoryHistory.Add(snapshot);
                    
                    // Keep only recent history
                    if (_memoryHistory.Count > _thresholds.MemoryHistorySize) {
                        _memoryHistory.RemoveAt(0);
                    }
                }
                
                // Log periodic monitoring status (every 5 minutes = 10 cycles of 30 seconds)
                if (_memoryHistory.Count % 10 == 0) {
                    var workingSetMB = snapshot.WorkingSet / 1024 / 1024;
                    var managedMB = snapshot.ManagedMemory / 1024 / 1024;
                    var growthMB = memoryGrowth / 1024 / 1024;
                    
                    Logger.Info($"Memory monitoring: Working Set: {workingSetMB}MB, Managed: {managedMB}MB, " +
                               $"Growth: {growthMB:+0;-0;0}MB, Snapshots: {_memoryHistory.Count}");
                    
                    CleanupDeadReferences();
                }
                
                // Check for concerning memory growth
                if (memoryGrowth > _thresholds.MaxMemoryGrowthMB * 1024 * 1024) {
                    Logger.Warn($"High memory growth detected: {memoryGrowth / 1024 / 1024}MB growth");
                    
                    // Trigger automatic health check
                    Task.Run(() => {
                        var report = PerformHealthCheck();
                        if (report.OverallHealth == MemoryHealthStatus.Critical) {
                            Logger.Error("Critical memory health detected during monitoring");
                        }
                    });
                }
                
                _lastGCMemory = currentMemory;
                
            } catch (Exception ex) {
                Logger.Error(ex, "Error during memory monitoring");
            }
        }

        private static MemoryPressureLevel GetMemoryPressureLevel() {
            try {
                var process = Process.GetCurrentProcess();
                var workingSetMB = process.WorkingSet64 / 1024 / 1024;
                
                if (workingSetMB > _thresholds.MaxWorkingSetMB) {
                    return MemoryPressureLevel.High;
                } else if (workingSetMB > _thresholds.MaxWorkingSetMB * 0.8) {
                    return MemoryPressureLevel.Medium;
                } else {
                    return MemoryPressureLevel.Low;
                }
            } catch {
                return MemoryPressureLevel.Unknown;
            }
        }

        private static void CheckMemoryGrowth(MemoryHealthReport report) {
            lock (_memoryHistoryLock) {
                if (_memoryHistory.Count < 2) return;
                
                var recent = _memoryHistory.Skip(Math.Max(0, _memoryHistory.Count - 5)).ToList();
                if (recent.Count < 2) return;
                
                var oldestRecent = recent.First();
                var newest = recent.Last();
                
                var timeDiff = newest.Timestamp - oldestRecent.Timestamp;
                var memoryGrowth = newest.ManagedMemory - oldestRecent.ManagedMemory;
                
                if (timeDiff.TotalMinutes > 0) {
                    var growthRateMBPerMinute = (memoryGrowth / 1024 / 1024) / timeDiff.TotalMinutes;
                    report.MemoryGrowthRateMBPerMinute = growthRateMBPerMinute;
                    
                    if (growthRateMBPerMinute > 1.0) { // More than 1MB per minute growth
                        report.Issues.Add($"High memory growth rate: {growthRateMBPerMinute:F2} MB/min");
                    }
                }
            }
        }

        private static void CheckTrackedObjectLeaks(MemoryHealthReport report) {
            var totalLeakedObjects = 0;
            
            foreach (var kvp in _trackedObjects) {
                var category = kvp.Key;
                var objects = kvp.Value;
                
                var objectArray = objects.ToArray();
                var aliveCount = objectArray.Count(wr => wr.Target != null);
                var deadCount = objectArray.Length - aliveCount;
                
                // Check for excessive alive objects
                if (aliveCount > 100) { // Threshold for concerning number of objects
                    report.Issues.Add($"High number of tracked objects in {category}: {aliveCount}");
                    totalLeakedObjects += aliveCount;
                }
                
                // Check for excessive dead references (indicates cleanup issues)
                if (deadCount > 50) {
                    report.Issues.Add($"High number of dead references in {category}: {deadCount}");
                }
            }
            
            report.PotentialLeakedObjects = totalLeakedObjects;
        }

        private static void CheckEventSubscriptionLeaks(MemoryHealthReport report) {
            #if DEBUG
            // Get event subscription statistics
            var eventStats = WeakEventManager.GetStatistics();
            
            if (eventStats.TotalDeadReferences > 20) {
                report.Issues.Add($"High number of dead event references: {eventStats.TotalDeadReferences}");
            }
            
            foreach (var eventInfo in eventStats.EventStats) {
                if (eventInfo.AliveSubscribers > 50) {
                    report.Issues.Add($"High number of subscribers for {eventInfo.EventName}: {eventInfo.AliveSubscribers}");
                }
            }
            #endif
        }

        private static void AssessOverallHealth(MemoryHealthReport report) {
            var criticalIssues = 0;
            var warningIssues = 0;
            
            // Check working set
            if (report.WorkingSetMB > _thresholds.MaxWorkingSetMB) {
                criticalIssues++;
            } else if (report.WorkingSetMB > _thresholds.MaxWorkingSetMB * 0.8) {
                warningIssues++;
            }
            
            // Check memory growth
            if (report.MemoryGrowthRateMBPerMinute > 2.0) {
                criticalIssues++;
            } else if (report.MemoryGrowthRateMBPerMinute > 1.0) {
                warningIssues++;
            }
            
            // Check leaked objects
            if (report.PotentialLeakedObjects > 200) {
                criticalIssues++;
            } else if (report.PotentialLeakedObjects > 100) {
                warningIssues++;
            }
            
            // Determine overall health
            if (criticalIssues > 0) {
                report.OverallHealth = MemoryHealthStatus.Critical;
            } else if (warningIssues > 1) {
                report.OverallHealth = MemoryHealthStatus.Warning;
            } else if (warningIssues > 0) {
                report.OverallHealth = MemoryHealthStatus.Caution;
            } else {
                report.OverallHealth = MemoryHealthStatus.Healthy;
            }
            
            _statistics._lastHealthCheck = report.CheckTime;
        }

        private class TrackedObjectWrapper {
            public object Target { get; set; }
            public string Description { get; set; }
            public DateTime TrackingTime { get; set; }
        }
    }

    /// <summary>
    /// Memory health monitoring thresholds.
    /// </summary>
    public class MemoryHealthThresholds {
        /// <summary>Maximum allowed memory growth in MB before triggering alerts.</summary>
        public double MaxMemoryGrowthMB { get; set; } = 50;
        
        /// <summary>Maximum allowed GC frequency per minute before triggering alerts.</summary>
        public int MaxGCFrequencyPerMinute { get; set; } = 10;
        
        /// <summary>Maximum allowed working set in MB before triggering alerts.</summary>
        public long MaxWorkingSetMB { get; set; } = 1024; // 1GB
        
        /// <summary>Number of memory snapshots to keep in history.</summary>
        public int MemoryHistorySize { get; set; } = 100;
    }

    /// <summary>
    /// Memory health report containing comprehensive health information.
    /// </summary>
    public class MemoryHealthReport {
        public DateTime CheckTime { get; set; }
        public int ProcessId { get; set; }
        public long WorkingSetMB { get; set; }
        public long PrivateMemoryMB { get; set; }
        public long ManagedMemoryMB { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
        public MemoryPressureLevel MemoryPressure { get; set; }
        public double MemoryGrowthRateMBPerMinute { get; set; }
        public int PotentialLeakedObjects { get; set; }
        public MemoryHealthStatus OverallHealth { get; set; }
        public List<string> Issues { get; set; } = new List<string>();
    }

    /// <summary>
    /// Memory leak detection statistics.
    /// </summary>
    public class MemoryLeakStatistics {
        internal long _totalObjectsTracked;
        internal long _totalObjectsUntracked;
        internal long _totalHealthChecks;
        internal long _criticalHealthChecks;
        internal DateTime _lastHealthCheck;
        
        public long TotalObjectsTracked => _totalObjectsTracked;
        public long TotalObjectsUntracked => _totalObjectsUntracked;
        public long TotalHealthChecks => _totalHealthChecks;
        public long CriticalHealthChecks => _criticalHealthChecks;
        public DateTime LastHealthCheck => _lastHealthCheck;
        public Dictionary<string, ObjectTrackingInfo> TrackedObjectsByCategory { get; set; } 
            = new Dictionary<string, ObjectTrackingInfo>();
    }

    /// <summary>
    /// Information about tracked objects in a category.
    /// </summary>
    public class ObjectTrackingInfo {
        public int AliveObjects { get; set; }
        public int DeadReferences { get; set; }
        public int TotalTracked { get; set; }
    }

    /// <summary>
    /// Memory snapshot for historical tracking.
    /// </summary>
    public class MemorySnapshot {
        public DateTime Timestamp { get; set; }
        public long ManagedMemory { get; set; }
        public long WorkingSet { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
    }

    /// <summary>
    /// Information about a detected memory leak.
    /// </summary>
    public class MemoryLeakInfo {
        public string Category { get; set; }
        public string ObjectType { get; set; }
        public string Description { get; set; }
        public TimeSpan Age { get; set; }
        public MemoryLeakType LeakType { get; set; }
    }

    /// <summary>
    /// Types of memory leaks that can be detected.
    /// </summary>
    public enum MemoryLeakType {
        LongLivedObject,
        EventHandlerLeak,
        CircularReference,
        UnreleasedResource
    }

    /// <summary>
    /// Memory pressure levels.
    /// </summary>
    public enum MemoryPressureLevel {
        Low,
        Medium,
        High,
        Unknown
    }

    /// <summary>
    /// Memory health status levels.
    /// </summary>
    public enum MemoryHealthStatus {
        Healthy,
        Caution,
        Warning,
        Critical,
        Error
    }
}
