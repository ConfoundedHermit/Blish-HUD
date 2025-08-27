using System;
using System.Collections.Generic;
using Blish_HUD.Controls;

namespace Blish_HUD._Extensions {
    
    /// <summary>
    /// Allocation-free extension methods for common LINQ operations.
    /// These methods are designed to reduce GC pressure by avoiding intermediate collections
    /// and iterator allocations in performance-critical paths.
    /// </summary>
    public static class CollectionOptimizationExtensions {
        
        /// <summary>
        /// Allocation-free Where operation that populates a provided result collection.
        /// Avoids creating intermediate enumerables and reduces GC pressure.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source collection</typeparam>
        /// <param name="source">The source collection to filter</param>
        /// <param name="predicate">The filter predicate</param>
        /// <param name="result">The collection to populate with filtered results (will be cleared first)</param>
        public static void WhereToList<T>(this IList<T> source, Func<T, bool> predicate, IList<T> result) {
            result.Clear();
            for (int i = 0; i < source.Count; i++) {
                if (predicate(source[i])) {
                    result.Add(source[i]);
                }
            }
        }
        
        /// <summary>
        /// Allocation-free Where operation that populates a provided result collection.
        /// Overload for IReadOnlyList to support more collection types.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source collection</typeparam>
        /// <param name="source">The source collection to filter</param>
        /// <param name="predicate">The filter predicate</param>
        /// <param name="result">The collection to populate with filtered results (will be cleared first)</param>
        public static void WhereToList<T>(this IReadOnlyList<T> source, Func<T, bool> predicate, IList<T> result) {
            result.Clear();
            for (int i = 0; i < source.Count; i++) {
                if (predicate(source[i])) {
                    result.Add(source[i]);
                }
            }
        }
        
        /// <summary>
        /// Allocation-free transformation operation that populates a provided result collection.
        /// Avoids creating intermediate enumerables and reduces GC pressure.
        /// </summary>
        /// <typeparam name="TSource">The type of elements in the source collection</typeparam>
        /// <typeparam name="TResult">The type of elements in the result collection</typeparam>
        /// <param name="source">The source collection to transform</param>
        /// <param name="selector">The transformation function</param>
        /// <param name="result">The collection to populate with transformed results (will be cleared first)</param>
        public static void SelectToList<TSource, TResult>(this IList<TSource> source, Func<TSource, TResult> selector, IList<TResult> result) {
            result.Clear();
            for (int i = 0; i < source.Count; i++) {
                result.Add(selector(source[i]));
            }
        }
        
        /// <summary>
        /// Allocation-free First operation using index-based search.
        /// Returns the first element that matches the predicate, or default(T) if none found.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source collection</typeparam>
        /// <param name="source">The source collection to search</param>
        /// <param name="predicate">The search predicate</param>
        /// <returns>The first matching element, or default(T) if none found</returns>
        public static T FirstOrDefaultFast<T>(this IList<T> source, Func<T, bool> predicate) {
            for (int i = 0; i < source.Count; i++) {
                if (predicate(source[i])) {
                    return source[i];
                }
            }
            return default(T);
        }
        
        /// <summary>
        /// Allocation-free First operation using index-based search.
        /// Overload for IReadOnlyList to support more collection types.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source collection</typeparam>
        /// <param name="source">The source collection to search</param>
        /// <param name="predicate">The search predicate</param>
        /// <returns>The first matching element, or default(T) if none found</returns>
        public static T FirstOrDefaultFast<T>(this IReadOnlyList<T> source, Func<T, bool> predicate) {
            for (int i = 0; i < source.Count; i++) {
                if (predicate(source[i])) {
                    return source[i];
                }
            }
            return default(T);
        }
        
        /// <summary>
        /// Allocation-free counting operation without enumeration.
        /// Counts elements that match the predicate using direct indexing.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source collection</typeparam>
        /// <param name="source">The source collection to count</param>
        /// <param name="predicate">The counting predicate</param>
        /// <returns>The number of elements that match the predicate</returns>
        public static int CountWhere<T>(this IList<T> source, Func<T, bool> predicate) {
            int count = 0;
            for (int i = 0; i < source.Count; i++) {
                if (predicate(source[i])) {
                    count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// Allocation-free counting operation without enumeration.
        /// Overload for IReadOnlyList to support more collection types.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source collection</typeparam>
        /// <param name="source">The source collection to count</param>
        /// <param name="predicate">The counting predicate</param>
        /// <returns>The number of elements that match the predicate</returns>
        public static int CountWhere<T>(this IReadOnlyList<T> source, Func<T, bool> predicate) {
            int count = 0;
            for (int i = 0; i < source.Count; i++) {
                if (predicate(source[i])) {
                    count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// Allocation-free Any operation using index-based search.
        /// Checks if any element matches the predicate without creating enumerators.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source collection</typeparam>
        /// <param name="source">The source collection to check</param>
        /// <param name="predicate">The matching predicate</param>
        /// <returns>True if any element matches the predicate, false otherwise</returns>
        public static bool AnyFast<T>(this IList<T> source, Func<T, bool> predicate) {
            for (int i = 0; i < source.Count; i++) {
                if (predicate(source[i])) {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Allocation-free All operation using index-based search.
        /// Checks if all elements match the predicate without creating enumerators.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source collection</typeparam>
        /// <param name="source">The source collection to check</param>
        /// <param name="predicate">The matching predicate</param>
        /// <returns>True if all elements match the predicate, false otherwise</returns>
        public static bool AllFast<T>(this IList<T> source, Func<T, bool> predicate) {
            for (int i = 0; i < source.Count; i++) {
                if (!predicate(source[i])) {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// Allocation-free Max operation for numeric values.
        /// Finds the maximum value using a selector function without creating enumerators.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source collection</typeparam>
        /// <param name="source">The source collection</param>
        /// <param name="selector">Function to extract the numeric value</param>
        /// <returns>The maximum value, or 0 if the collection is empty</returns>
        public static int MaxFast<T>(this IList<T> source, Func<T, int> selector) {
            if (source.Count == 0) return 0;
            
            int max = selector(source[0]);
            for (int i = 1; i < source.Count; i++) {
                int value = selector(source[i]);
                if (value > max) {
                    max = value;
                }
            }
            return max;
        }
        
        /// <summary>
        /// Allocation-free Min operation for numeric values.
        /// Finds the minimum value using a selector function without creating enumerators.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source collection</typeparam>
        /// <param name="source">The source collection</param>
        /// <param name="selector">Function to extract the numeric value</param>
        /// <returns>The minimum value, or 0 if the collection is empty</returns>
        public static int MinFast<T>(this IList<T> source, Func<T, int> selector) {
            if (source.Count == 0) return 0;
            
            int min = selector(source[0]);
            for (int i = 1; i < source.Count; i++) {
                int value = selector(source[i]);
                if (value < min) {
                    min = value;
                }
            }
            return min;
        }
        
        /// <summary>
        /// Allocation-free combined Where and Select operation.
        /// Filters and transforms elements in a single pass without intermediate collections.
        /// </summary>
        /// <typeparam name="TSource">The type of elements in the source collection</typeparam>
        /// <typeparam name="TResult">The type of elements in the result collection</typeparam>
        /// <param name="source">The source collection</param>
        /// <param name="predicate">The filter predicate</param>
        /// <param name="selector">The transformation function</param>
        /// <param name="result">The collection to populate with filtered and transformed results</param>
        public static void WhereSelectToList<TSource, TResult>(this IList<TSource> source, 
                                                               Func<TSource, bool> predicate, 
                                                               Func<TSource, TResult> selector, 
                                                               IList<TResult> result) {
            result.Clear();
            for (int i = 0; i < source.Count; i++) {
                var item = source[i];
                if (predicate(item)) {
                    result.Add(selector(item));
                }
            }
        }
        
        /// <summary>
        /// Allocation-free operation to copy visible controls to a result collection.
        /// Specialized method for the common UI pattern of filtering visible controls.
        /// </summary>
        /// <param name="source">The source collection of controls</param>
        /// <param name="result">The collection to populate with visible controls</param>
        public static void GetVisibleControls(this IList<Control> source, IList<Control> result) {
            result.Clear();
            for (int i = 0; i < source.Count; i++) {
                var control = source[i];
                if (control.Visible) {
                    result.Add(control);
                }
            }
        }
        
        /// <summary>
        /// Allocation-free operation to find controls of a specific type.
        /// Specialized method for the common UI pattern of finding typed controls.
        /// </summary>
        /// <typeparam name="T">The type of control to find</typeparam>
        /// <param name="source">The source collection of controls</param>
        /// <param name="result">The collection to populate with matching controls</param>
        public static void GetControlsOfType<T>(this IList<Control> source, IList<T> result) where T : Control {
            result.Clear();
            for (int i = 0; i < source.Count; i++) {
                if (source[i] is T typedControl) {
                    result.Add(typedControl);
                }
            }
        }
    }
}
