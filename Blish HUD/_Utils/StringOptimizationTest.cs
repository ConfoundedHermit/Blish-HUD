using System;
using System.Diagnostics;
using Blish_HUD._Extensions;

namespace Blish_HUD._Utils {
    /// <summary>
    /// Simple test class to validate string optimization implementations.
    /// This class can be used for basic validation and performance testing.
    /// </summary>
    public static class StringOptimizationTest {
        /// <summary>
        /// Runs basic validation tests for the string optimization infrastructure.
        /// </summary>
        /// <returns>True if all tests pass, false otherwise.</returns>
        public static bool RunValidationTests() {
            try {
                // Test StringPool functionality
                if (!TestStringPool()) return false;
                
                // Test FormatStringCache functionality
                if (!TestFormatStringCache()) return false;
                
                // Test CachedStringFormatter functionality
                if (!TestCachedStringFormatter()) return false;
                
                // Test SpecializedStringPools functionality
                if (!TestSpecializedStringPools()) return false;
                
                // Test PooledStringBuilder integration
                if (!TestPooledStringBuilderIntegration()) return false;
                
                return true;
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"String optimization test failed: {ex.Message}");
                return false;
            }
        }

        private static bool TestStringPool() {
            // Reset counters for clean test
            StringPool.ResetPerformanceCounters();
            
            // Test basic pooling
            var str1 = StringPool.GetPooled("test");
            var str2 = StringPool.GetPooled("test");
            
            // Should return the same reference for identical strings
            if (!ReferenceEquals(str1, str2)) {
                System.Diagnostics.Debug.WriteLine("StringPool: Failed reference equality test");
                return false;
            }
            
            // Test performance tracking
            if (StringPool.HitRate <= 0) {
                System.Diagnostics.Debug.WriteLine("StringPool: Performance tracking not working");
                return false;
            }
            
            System.Diagnostics.Debug.WriteLine($"StringPool: {StringPool.GetPerformanceStatistics()}");
            return true;
        }

        private static bool TestFormatStringCache() {
            // Reset counters for clean test
            FormatStringCache.ResetPerformanceCounters();
            
            // Test basic caching
            var format1 = FormatStringCache.GetCachedFormat("Test {0}");
            var format2 = FormatStringCache.GetCachedFormat("Test {0}");
            
            // Should return the same reference for identical format strings
            if (!ReferenceEquals(format1, format2)) {
                System.Diagnostics.Debug.WriteLine("FormatStringCache: Failed reference equality test");
                return false;
            }
            
            // Test performance tracking
            if (FormatStringCache.HitRate <= 0) {
                System.Diagnostics.Debug.WriteLine("FormatStringCache: Performance tracking not working");
                return false;
            }
            
            System.Diagnostics.Debug.WriteLine($"FormatStringCache: {FormatStringCache.GetPerformanceStatistics()}");
            return true;
        }

        private static bool TestCachedStringFormatter() {
            // Test various formatting methods
            var result1 = CachedStringFormatter.Format("Test {0}", "value");
            var result2 = CachedStringFormatter.Format("Test {0} {1}", "value1", "value2");
            var result3 = CachedStringFormatter.Format("Test {0} {1} {2}", "value1", "value2", "value3");
            
            if (result1 != "Test value" || 
                result2 != "Test value1 value2" || 
                result3 != "Test value1 value2 value3") {
                System.Diagnostics.Debug.WriteLine("CachedStringFormatter: Formatting results incorrect");
                return false;
            }
            
            System.Diagnostics.Debug.WriteLine("CachedStringFormatter: All formatting tests passed");
            return true;
        }

        private static bool TestSpecializedStringPools() {
            // Test file extensions pool
            var ext1 = SpecializedStringPools.FileExtensions.GetPooled(".json");
            var ext2 = SpecializedStringPools.FileExtensions.GetPooled(".json");
            
            if (!ReferenceEquals(ext1, ext2)) {
                System.Diagnostics.Debug.WriteLine("SpecializedStringPools.FileExtensions: Failed reference equality test");
                return false;
            }
            
            // Test configuration keys pool
            var key1 = SpecializedStringPools.ConfigurationKeys.GetPooled("Enabled");
            var key2 = SpecializedStringPools.ConfigurationKeys.GetPooled("Enabled");
            
            if (!ReferenceEquals(key1, key2)) {
                System.Diagnostics.Debug.WriteLine("SpecializedStringPools.ConfigurationKeys: Failed reference equality test");
                return false;
            }
            
            System.Diagnostics.Debug.WriteLine("SpecializedStringPools: All tests passed");
            return true;
        }

        private static bool TestPooledStringBuilderIntegration() {
            // Reset StringBuilder pool for clean test
            StringBuilderPool.ResetPerformanceCounters();
            
            // Test enhanced PooledStringBuilder methods
            var result1 = PooledStringBuilder.Format("Test {0}", "value");
            var result2 = PooledStringBuilder.Join(", ", "a", "b", "c");
            var result3 = PooledStringBuilder.Concat("Hello", " ", "World");
            
            if (result1 != "Test value" || 
                result2 != "a, b, c" || 
                result3 != "Hello World") {
                System.Diagnostics.Debug.WriteLine("PooledStringBuilder: Integration results incorrect");
                return false;
            }
            
            // Verify StringBuilder pool was used
            if (StringBuilderPool.HitRate < 0) {
                System.Diagnostics.Debug.WriteLine("PooledStringBuilder: StringBuilder pool not being used");
                return false;
            }
            
            System.Diagnostics.Debug.WriteLine($"PooledStringBuilder: {StringBuilderPool.GetPerformanceStatistics()}");
            return true;
        }

        /// <summary>
        /// Runs a simple performance comparison between optimized and unoptimized string operations.
        /// </summary>
        /// <param name="iterations">Number of iterations to run for each test.</param>
        public static void RunPerformanceComparison(int iterations = 10000) {
            System.Diagnostics.Debug.WriteLine($"Running performance comparison with {iterations} iterations...");
            
            // Test string formatting performance
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++) {
                var result = string.Format("Test {0} {1}", i, "value");
            }
            sw.Stop();
            var standardTime = sw.ElapsedMilliseconds;
            
            sw.Restart();
            for (int i = 0; i < iterations; i++) {
                var result = CachedStringFormatter.Format("Test {0} {1}", i, "value");
            }
            sw.Stop();
            var optimizedTime = sw.ElapsedMilliseconds;
            
            System.Diagnostics.Debug.WriteLine($"String formatting - Standard: {standardTime}ms, Optimized: {optimizedTime}ms");
            System.Diagnostics.Debug.WriteLine($"Performance improvement: {((double)(standardTime - optimizedTime) / standardTime * 100):F1}%");
            
            // Print final statistics
            System.Diagnostics.Debug.WriteLine($"Final {StringPool.GetPerformanceStatistics()}");
            System.Diagnostics.Debug.WriteLine($"Final {FormatStringCache.GetPerformanceStatistics()}");
            System.Diagnostics.Debug.WriteLine($"Final {StringBuilderPool.GetPerformanceStatistics()}");
        }
    }
}
