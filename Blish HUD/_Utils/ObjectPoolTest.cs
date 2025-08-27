using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace Blish_HUD._Utils {
    /// <summary>
    /// Simple test class to validate the object pool implementations for Task 1.3.3.3.
    /// This class provides basic functionality tests for the newly implemented pools.
    /// </summary>
    public static class ObjectPoolTest {
        
        /// <summary>
        /// Runs basic tests on all implemented object pools to validate functionality.
        /// </summary>
        /// <returns>A test report indicating success or failure of each pool test.</returns>
        public static string RunBasicTests() {
            var report = "=== OBJECT POOL IMPLEMENTATION TEST REPORT ===\n";
            report += $"Test Run Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\n";
            
            bool allTestsPassed = true;
            
            // Test GeometryPool
            try {
                report += "--- GeometryPool Tests ---\n";
                
                // Test PointWrapper
                var pointWrapper = GeometryPool.GetPointWrapper(10, 20);
                if (pointWrapper.X == 10 && pointWrapper.Y == 20) {
                    report += "✅ PointWrapper creation and access: PASSED\n";
                } else {
                    report += "❌ PointWrapper creation and access: FAILED\n";
                    allTestsPassed = false;
                }
                GeometryPool.ReturnPointWrapper(pointWrapper);
                
                // Test RectangleWrapper
                var rectWrapper = GeometryPool.GetRectangleWrapper(5, 10, 100, 200);
                if (rectWrapper.X == 5 && rectWrapper.Y == 10 && rectWrapper.Width == 100 && rectWrapper.Height == 200) {
                    report += "✅ RectangleWrapper creation and access: PASSED\n";
                } else {
                    report += "❌ RectangleWrapper creation and access: FAILED\n";
                    allTestsPassed = false;
                }
                GeometryPool.ReturnRectangleWrapper(rectWrapper);
                
                // Test Vector2Wrapper
                var vectorWrapper = GeometryPool.GetVector2Wrapper(1.5f, 2.5f);
                if (Math.Abs(vectorWrapper.X - 1.5f) < 0.001f && Math.Abs(vectorWrapper.Y - 2.5f) < 0.001f) {
                    report += "✅ Vector2Wrapper creation and access: PASSED\n";
                } else {
                    report += "❌ Vector2Wrapper creation and access: FAILED\n";
                    allTestsPassed = false;
                }
                GeometryPool.ReturnVector2Wrapper(vectorWrapper);
                
                // Test pooled objects with using statement
                using (var pooledPoint = GeometryPoolExtensions.GetPooledPoint(30, 40)) {
                    if (pooledPoint.Value.X == 30 && pooledPoint.Value.Y == 40) {
                        report += "✅ Pooled PointWrapper with automatic disposal: PASSED\n";
                    } else {
                        report += "❌ Pooled PointWrapper with automatic disposal: FAILED\n";
                        allTestsPassed = false;
                    }
                }
                
                report += $"GeometryPool Statistics: {GeometryPool.GetPerformanceStatistics()}\n\n";
                
            } catch (Exception ex) {
                report += $"❌ GeometryPool tests failed with exception: {ex.Message}\n\n";
                allTestsPassed = false;
            }
            
            // Test StreamPool
            try {
                report += "--- StreamPool Tests ---\n";
                
                // Test MemoryStream
                var memoryStream = StreamPool.GetMemoryStream();
                memoryStream.WriteByte(42);
                if (memoryStream.Length == 1 && memoryStream.Position == 1) {
                    report += "✅ MemoryStream creation and write: PASSED\n";
                } else {
                    report += "❌ MemoryStream creation and write: FAILED\n";
                    allTestsPassed = false;
                }
                StreamPool.ReturnMemoryStream(memoryStream);
                
                // Test StringWriter
                var stringWriter = StreamPool.GetStringWriter();
                stringWriter.Write("Hello, World!");
                if (stringWriter.ToString() == "Hello, World!") {
                    report += "✅ StringWriter creation and write: PASSED\n";
                } else {
                    report += "❌ StringWriter creation and write: FAILED\n";
                    allTestsPassed = false;
                }
                StreamPool.ReturnStringWriter(stringWriter);
                
                // Test BinaryWriter factory
                var binaryWriter = StreamPool.GetBinaryWriter();
                binaryWriter.Write(123);
                binaryWriter.Write("test");
                if (binaryWriter.BaseStream.Length > 0) {
                    report += "✅ BinaryWriter factory creation and write: PASSED\n";
                } else {
                    report += "❌ BinaryWriter factory creation and write: FAILED\n";
                    allTestsPassed = false;
                }
                StreamPool.ReturnBinaryWriter(binaryWriter);
                
                // Test pooled objects with using statement
                using (var pooledMemoryStream = StreamPoolExtensions.GetPooledMemoryStream()) {
                    pooledMemoryStream.Value.WriteByte(99);
                    if (pooledMemoryStream.Value.Length == 1) {
                        report += "✅ Pooled MemoryStream with automatic disposal: PASSED\n";
                    } else {
                        report += "❌ Pooled MemoryStream with automatic disposal: FAILED\n";
                        allTestsPassed = false;
                    }
                }
                
                report += $"StreamPool Statistics: {StreamPool.GetPerformanceStatistics()}\n\n";
                
            } catch (Exception ex) {
                report += $"❌ StreamPool tests failed with exception: {ex.Message}\n\n";
                allTestsPassed = false;
            }
            
            // Test PoolMonitor integration
            try {
                report += "--- PoolMonitor Integration Tests ---\n";
                
                var comprehensiveStats = PoolMonitor.GetComprehensiveStatistics();
                if (comprehensiveStats.Contains("Geometry Pool") && comprehensiveStats.Contains("Stream Pool")) {
                    report += "✅ PoolMonitor includes new pools: PASSED\n";
                } else {
                    report += "❌ PoolMonitor includes new pools: FAILED\n";
                    allTestsPassed = false;
                }
                
                var diagnostics = PoolMonitor.RunDiagnostics();
                if (diagnostics.Contains("Geometry Pool") && diagnostics.Contains("Stream Pool")) {
                    report += "✅ PoolMonitor diagnostics include new pools: PASSED\n";
                } else {
                    report += "❌ PoolMonitor diagnostics include new pools: FAILED\n";
                    allTestsPassed = false;
                }
                
                report += "\n";
                
            } catch (Exception ex) {
                report += $"❌ PoolMonitor integration tests failed with exception: {ex.Message}\n\n";
                allTestsPassed = false;
            }
            
            // Final summary
            report += "=== TEST SUMMARY ===\n";
            if (allTestsPassed) {
                report += "✅ ALL TESTS PASSED - Task 1.3.3.3 implementation is functional\n";
            } else {
                report += "❌ SOME TESTS FAILED - Review implementation for issues\n";
            }
            
            report += $"Test completed at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n";
            report += "=== END TEST REPORT ===\n";
            
            return report;
        }
        
        /// <summary>
        /// Runs performance tests to validate pool efficiency.
        /// </summary>
        /// <returns>A performance test report.</returns>
        public static string RunPerformanceTests() {
            var report = "=== OBJECT POOL PERFORMANCE TEST REPORT ===\n";
            report += $"Performance Test Run Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\n";
            
            const int iterations = 1000;
            
            // Reset pool statistics for clean measurement
            GeometryPool.ResetPerformanceCounters();
            StreamPool.ResetPerformanceCounters();
            
            try {
                report += $"--- Performance Test ({iterations} iterations) ---\n";
                
                // Test GeometryPool performance
                var startTime = DateTime.UtcNow;
                for (int i = 0; i < iterations; i++) {
                    var point = GeometryPool.GetPointWrapper(i, i * 2);
                    GeometryPool.ReturnPointWrapper(point);
                    
                    var rect = GeometryPool.GetRectangleWrapper(i, i, i * 2, i * 3);
                    GeometryPool.ReturnRectangleWrapper(rect);
                    
                    var vector = GeometryPool.GetVector2Wrapper(i * 0.1f, i * 0.2f);
                    GeometryPool.ReturnVector2Wrapper(vector);
                }
                var geometryTime = DateTime.UtcNow - startTime;
                
                // Test StreamPool performance
                startTime = DateTime.UtcNow;
                for (int i = 0; i < iterations; i++) {
                    var stream = StreamPool.GetMemoryStream();
                    stream.WriteByte((byte)(i % 256));
                    StreamPool.ReturnMemoryStream(stream);
                    
                    var writer = StreamPool.GetStringWriter();
                    writer.Write($"Test {i}");
                    StreamPool.ReturnStringWriter(writer);
                }
                var streamTime = DateTime.UtcNow - startTime;
                
                report += $"GeometryPool {iterations} operations completed in: {geometryTime.TotalMilliseconds:F2}ms\n";
                report += $"StreamPool {iterations} operations completed in: {streamTime.TotalMilliseconds:F2}ms\n\n";
                
                // Report final statistics
                report += "--- Final Pool Statistics ---\n";
                report += $"GeometryPool: {GeometryPool.GetPerformanceStatistics()}\n\n";
                report += $"StreamPool: {StreamPool.GetPerformanceStatistics()}\n\n";
                
                report += "✅ Performance tests completed successfully\n";
                
            } catch (Exception ex) {
                report += $"❌ Performance tests failed with exception: {ex.Message}\n";
            }
            
            report += "=== END PERFORMANCE TEST REPORT ===\n";
            return report;
        }
    }
}
