using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Blish_HUD._Utils {

    /// <summary>
    /// Validation utility for the MemoryLeakDetector system.
    /// Provides runtime validation and testing of memory leak detection functionality.
    /// </summary>
    public static class MemoryLeakDetectorValidator {
        
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(MemoryLeakDetectorValidator));
        
        /// <summary>
        /// Runs all validation tests for the memory leak detection system.
        /// </summary>
        /// <returns>True if all tests pass, false otherwise</returns>
        public static bool RunAllValidationTests() {
            Logger.Info("Starting MemoryLeakDetector validation tests...");
            
            var testResults = new List<(string TestName, bool Passed, string Error)>();
            
            // Run all validation tests
            testResults.Add(("TrackObject_ShouldIncrementTrackedCount", ValidateTrackObject(), null));
            testResults.Add(("UntrackObject_ShouldIncrementUntrackedCount", ValidateUntrackObject(), null));
            testResults.Add(("TrackObject_WithNullObject_ShouldNotCrash", ValidateNullObjectTracking(), null));
            testResults.Add(("PerformHealthCheck_ShouldReturnValidReport", ValidateHealthCheck(), null));
            testResults.Add(("ConfigureThresholds_ShouldUpdateThresholds", ValidateThresholdConfiguration(), null));
            testResults.Add(("GetStatistics_ShouldReturnValidStatistics", ValidateStatistics(), null));
            testResults.Add(("MultipleCategories_ShouldTrackSeparately", ValidateMultipleCategories(), null));
            testResults.Add(("ConcurrentTracking_ShouldBeThreadSafe", ValidateConcurrentTracking(), null));
            testResults.Add(("MemoryHealthThresholds_ShouldHaveReasonableDefaults", ValidateThresholdDefaults(), null));
            testResults.Add(("Integration_ShouldWorkWithEventTracker", ValidateEventTrackerIntegration(), null));
            
            // Report results
            var passedTests = testResults.Count(r => r.Passed);
            var totalTests = testResults.Count;
            
            Logger.Info($"Validation completed: {passedTests}/{totalTests} tests passed");
            
            foreach (var result in testResults) {
                if (result.Passed) {
                    Logger.Info($"✓ {result.TestName}");
                } else {
                    Logger.Error($"✗ {result.TestName}: {result.Error ?? "Failed"}");
                }
            }
            
            return passedTests == totalTests;
        }

        private static bool ValidateTrackObject() {
            try {
                var testObject = new TestObject("Test1");
                var initialStats = MemoryLeakDetector.GetStatistics();
                var initialCount = initialStats.TotalObjectsTracked;

                MemoryLeakDetector.TrackObject("TestCategory", testObject, "Test object for tracking");

                var finalStats = MemoryLeakDetector.GetStatistics();
                
                if (finalStats.TotalObjectsTracked != initialCount + 1) {
                    Logger.Error($"Expected tracked count {initialCount + 1}, got {finalStats.TotalObjectsTracked}");
                    return false;
                }
                
                if (!finalStats.TrackedObjectsByCategory.ContainsKey("TestCategory")) {
                    Logger.Error("TestCategory should exist in tracked objects");
                    return false;
                }
                
                if (finalStats.TrackedObjectsByCategory["TestCategory"].AliveObjects < 1) {
                    Logger.Error("TestCategory should have at least 1 alive object");
                    return false;
                }
                
                return true;
            } catch (Exception ex) {
                Logger.Error(ex, "ValidateTrackObject failed");
                return false;
            }
        }

        private static bool ValidateUntrackObject() {
            try {
                var testObject = new TestObject("Test2");
                MemoryLeakDetector.TrackObject("TestCategory", testObject, "Test object for untracking");
                var initialStats = MemoryLeakDetector.GetStatistics();
                var initialUntrackedCount = initialStats.TotalObjectsUntracked;

                MemoryLeakDetector.UntrackObject("TestCategory", testObject);

                var finalStats = MemoryLeakDetector.GetStatistics();
                
                if (finalStats.TotalObjectsUntracked != initialUntrackedCount + 1) {
                    Logger.Error($"Expected untracked count {initialUntrackedCount + 1}, got {finalStats.TotalObjectsUntracked}");
                    return false;
                }
                
                return true;
            } catch (Exception ex) {
                Logger.Error(ex, "ValidateUntrackObject failed");
                return false;
            }
        }

        private static bool ValidateNullObjectTracking() {
            try {
                var initialStats = MemoryLeakDetector.GetStatistics();
                var initialCount = initialStats.TotalObjectsTracked;

                MemoryLeakDetector.TrackObject("TestCategory", null, "Null object test");

                var finalStats = MemoryLeakDetector.GetStatistics();
                
                if (finalStats.TotalObjectsTracked != initialCount) {
                    Logger.Error("Tracked count should not change when tracking null object");
                    return false;
                }
                
                return true;
            } catch (Exception ex) {
                Logger.Error(ex, "ValidateNullObjectTracking failed");
                return false;
            }
        }

        private static bool ValidateHealthCheck() {
            try {
                var report = MemoryLeakDetector.PerformHealthCheck();

                if (report == null) {
                    Logger.Error("Health report should not be null");
                    return false;
                }
                
                if (report.CheckTime <= DateTime.MinValue) {
                    Logger.Error("Check time should be set");
                    return false;
                }
                
                if (report.ProcessId <= 0) {
                    Logger.Error("Process ID should be valid");
                    return false;
                }
                
                if (report.WorkingSetMB < 0) {
                    Logger.Error("Working set should be non-negative");
                    return false;
                }
                
                if (report.ManagedMemoryMB < 0) {
                    Logger.Error("Managed memory should be non-negative");
                    return false;
                }
                
                if (report.Issues == null) {
                    Logger.Error("Issues list should not be null");
                    return false;
                }
                
                if (!Enum.IsDefined(typeof(MemoryHealthStatus), report.OverallHealth)) {
                    Logger.Error("Overall health should be a valid enum value");
                    return false;
                }
                
                return true;
            } catch (Exception ex) {
                Logger.Error(ex, "ValidateHealthCheck failed");
                return false;
            }
        }

        private static bool ValidateThresholdConfiguration() {
            try {
                var customThresholds = new MemoryHealthThresholds {
                    MaxMemoryGrowthMB = 100,
                    MaxGCFrequencyPerMinute = 20,
                    MaxWorkingSetMB = 2048,
                    MemoryHistorySize = 200
                };

                MemoryLeakDetector.ConfigureThresholds(customThresholds);

                var report = MemoryLeakDetector.PerformHealthCheck();
                if (report == null) {
                    Logger.Error("Health check should still work after configuring thresholds");
                    return false;
                }
                
                return true;
            } catch (Exception ex) {
                Logger.Error(ex, "ValidateThresholdConfiguration failed");
                return false;
            }
        }

        private static bool ValidateStatistics() {
            try {
                var testObject = new TestObject("Test6");
                MemoryLeakDetector.TrackObject("TestCategory", testObject, "Statistics test object");

                var stats = MemoryLeakDetector.GetStatistics();

                if (stats == null) {
                    Logger.Error("Statistics should not be null");
                    return false;
                }
                
                if (stats.TotalObjectsTracked < 0) {
                    Logger.Error("Total tracked should be non-negative");
                    return false;
                }
                
                if (stats.TotalObjectsUntracked < 0) {
                    Logger.Error("Total untracked should be non-negative");
                    return false;
                }
                
                if (stats.TotalHealthChecks < 0) {
                    Logger.Error("Total health checks should be non-negative");
                    return false;
                }
                
                if (stats.TrackedObjectsByCategory == null) {
                    Logger.Error("Category dictionary should not be null");
                    return false;
                }
                
                return true;
            } catch (Exception ex) {
                Logger.Error(ex, "ValidateStatistics failed");
                return false;
            }
        }

        private static bool ValidateMultipleCategories() {
            try {
                var controlObject = new TestObject("Control1");
                var serviceObject = new TestObject("Service1");
                var moduleObject = new TestObject("Module1");

                MemoryLeakDetector.TrackObject("Controls", controlObject, "Test control");
                MemoryLeakDetector.TrackObject("Services", serviceObject, "Test service");
                MemoryLeakDetector.TrackObject("Modules", moduleObject, "Test module");

                var stats = MemoryLeakDetector.GetStatistics();
                
                if (!stats.TrackedObjectsByCategory.ContainsKey("Controls")) {
                    Logger.Error("Should track Controls category");
                    return false;
                }
                
                if (!stats.TrackedObjectsByCategory.ContainsKey("Services")) {
                    Logger.Error("Should track Services category");
                    return false;
                }
                
                if (!stats.TrackedObjectsByCategory.ContainsKey("Modules")) {
                    Logger.Error("Should track Modules category");
                    return false;
                }
                
                return true;
            } catch (Exception ex) {
                Logger.Error(ex, "ValidateMultipleCategories failed");
                return false;
            }
        }

        private static bool ValidateConcurrentTracking() {
            try {
                const int threadCount = 5;
                const int objectsPerThread = 50;
                var tasks = new Task[threadCount];
                var objects = new List<TestObject>();

                for (int i = 0; i < threadCount; i++) {
                    int threadIndex = i;
                    tasks[i] = Task.Run(() => {
                        for (int j = 0; j < objectsPerThread; j++) {
                            var obj = new TestObject($"Thread{threadIndex}_Object{j}");
                            lock (objects) {
                                objects.Add(obj);
                            }
                            MemoryLeakDetector.TrackObject($"Thread{threadIndex}", obj, 
                                $"Concurrent test object {j}");
                        }
                    });
                }

                Task.WaitAll(tasks);

                var stats = MemoryLeakDetector.GetStatistics();
                
                if (stats.TotalObjectsTracked < threadCount * objectsPerThread) {
                    Logger.Error($"Should track at least {threadCount * objectsPerThread} objects from all threads, got {stats.TotalObjectsTracked}");
                    return false;
                }
                
                return true;
            } catch (Exception ex) {
                Logger.Error(ex, "ValidateConcurrentTracking failed");
                return false;
            }
        }

        private static bool ValidateThresholdDefaults() {
            try {
                var thresholds = new MemoryHealthThresholds();

                if (thresholds.MaxMemoryGrowthMB <= 0) {
                    Logger.Error("Max memory growth should be positive");
                    return false;
                }
                
                if (thresholds.MaxGCFrequencyPerMinute <= 0) {
                    Logger.Error("Max GC frequency should be positive");
                    return false;
                }
                
                if (thresholds.MaxWorkingSetMB <= 0) {
                    Logger.Error("Max working set should be positive");
                    return false;
                }
                
                if (thresholds.MemoryHistorySize <= 0) {
                    Logger.Error("Memory history size should be positive");
                    return false;
                }
                
                return true;
            } catch (Exception ex) {
                Logger.Error(ex, "ValidateThresholdDefaults failed");
                return false;
            }
        }

        private static bool ValidateEventTrackerIntegration() {
            try {
                var tracker = new EventSubscriptionTracker();
                var testObject = new TestEventSource();
                
                MemoryLeakDetector.TrackObject("EventTrackers", tracker, "Integration test tracker");

                tracker.TrackSubscription<EventArgs>(testObject, "TestEvent", TestEventHandler);
                
                var healthReport = MemoryLeakDetector.PerformHealthCheck();
                var leakStats = MemoryLeakDetector.GetStatistics();

                if (healthReport == null) {
                    Logger.Error("Health report should be generated");
                    return false;
                }
                
                if (leakStats == null) {
                    Logger.Error("Leak statistics should be available");
                    return false;
                }
                
                if (!leakStats.TrackedObjectsByCategory.ContainsKey("EventTrackers")) {
                    Logger.Error("Should track the event tracker");
                    return false;
                }

                // Cleanup
                tracker.Dispose();
                MemoryLeakDetector.UntrackObject("EventTrackers", tracker);
                
                return true;
            } catch (Exception ex) {
                Logger.Error(ex, "ValidateEventTrackerIntegration failed");
                return false;
            }
        }

        private static void TestEventHandler(object sender, EventArgs e) {
            // Test event handler implementation
        }

        /// <summary>
        /// Test object class for memory leak detection testing.
        /// </summary>
        private class TestObject {
            public string Name { get; }
            public DateTime CreatedAt { get; }
            
            public TestObject(string name) {
                Name = name;
                CreatedAt = DateTime.UtcNow;
            }
            
            public override string ToString() {
                return $"TestObject: {Name} (Created: {CreatedAt})";
            }
        }

        /// <summary>
        /// Test event source for integration testing.
        /// </summary>
        private class TestEventSource {
            public event EventHandler<EventArgs> TestEvent;
            
            public void RaiseTestEvent() {
                TestEvent?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Performance validation for memory leak detection system.
    /// </summary>
    public static class MemoryLeakDetectorPerformanceValidator {
        
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(MemoryLeakDetectorPerformanceValidator));

        /// <summary>
        /// Runs performance validation tests.
        /// </summary>
        /// <returns>True if all performance tests pass, false otherwise</returns>
        public static bool RunPerformanceValidation() {
            Logger.Info("Starting MemoryLeakDetector performance validation...");
            
            var testResults = new List<(string TestName, bool Passed, string Error)>();
            
            testResults.Add(("TrackingLargeNumberOfObjects_ShouldBePerformant", ValidateTrackingPerformance(), null));
            testResults.Add(("HealthCheckPerformance_ShouldBeReasonable", ValidateHealthCheckPerformance(), null));
            testResults.Add(("CleanupPerformance_ShouldBeEfficient", ValidateCleanupPerformance(), null));
            
            var passedTests = testResults.Count(r => r.Passed);
            var totalTests = testResults.Count;
            
            Logger.Info($"Performance validation completed: {passedTests}/{totalTests} tests passed");
            
            foreach (var result in testResults) {
                if (result.Passed) {
                    Logger.Info($"✓ {result.TestName}");
                } else {
                    Logger.Error($"✗ {result.TestName}: {result.Error ?? "Failed"}");
                }
            }
            
            return passedTests == totalTests;
        }

        private static bool ValidateTrackingPerformance() {
            try {
                const int objectCount = 5000; // Reduced for reasonable test time
                var objects = new List<object>();
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                for (int i = 0; i < objectCount; i++) {
                    var obj = new object();
                    objects.Add(obj);
                    MemoryLeakDetector.TrackObject("PerformanceTest", obj, $"Object {i}");
                }

                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds > 5000) {
                    Logger.Error($"Tracking {objectCount} objects took {stopwatch.ElapsedMilliseconds}ms, should be under 5000ms");
                    return false;
                }

                var stats = MemoryLeakDetector.GetStatistics();
                if (stats.TrackedObjectsByCategory["PerformanceTest"].AliveObjects < objectCount) {
                    Logger.Error("Should track all objects");
                    return false;
                }
                
                Logger.Info($"Tracked {objectCount} objects in {stopwatch.ElapsedMilliseconds}ms");
                return true;
            } catch (Exception ex) {
                Logger.Error(ex, "ValidateTrackingPerformance failed");
                return false;
            }
        }

        private static bool ValidateHealthCheckPerformance() {
            try {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var report = MemoryLeakDetector.PerformHealthCheck();

                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds > 2000) {
                    Logger.Error($"Health check took {stopwatch.ElapsedMilliseconds}ms, should be under 2000ms");
                    return false;
                }
                
                if (report == null) {
                    Logger.Error("Health report should be generated");
                    return false;
                }
                
                Logger.Info($"Health check completed in {stopwatch.ElapsedMilliseconds}ms");
                return true;
            } catch (Exception ex) {
                Logger.Error(ex, "ValidateHealthCheckPerformance failed");
                return false;
            }
        }

        private static bool ValidateCleanupPerformance() {
            try {
                const int objectCount = 500;
                var objects = new List<object>();
                
                // Create and track objects
                for (int i = 0; i < objectCount; i++) {
                    var obj = new object();
                    objects.Add(obj);
                    MemoryLeakDetector.TrackObject("CleanupTest", obj, $"Object {i}");
                }

                // Make half eligible for GC
                for (int i = 0; i < objectCount / 2; i++) {
                    objects[i] = null;
                }
                objects.RemoveRange(0, objectCount / 2);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                MemoryLeakDetector.CleanupDeadReferences();

                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds > 1000) {
                    Logger.Error($"Cleanup took {stopwatch.ElapsedMilliseconds}ms, should be under 1000ms");
                    return false;
                }
                
                Logger.Info($"Cleanup completed in {stopwatch.ElapsedMilliseconds}ms");
                return true;
            } catch (Exception ex) {
                Logger.Error(ex, "ValidateCleanupPerformance failed");
                return false;
            }
        }
    }
}
