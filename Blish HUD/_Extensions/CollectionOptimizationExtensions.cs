using System;
using System.Collections.Generic;

namespace Blish_HUD._Extensions {

    /// <summary>
    /// Provides allocation-free extension methods for common collection operations
    /// to reduce garbage collection pressure and improve performance.
    /// </summary>
    public static class CollectionOptimizationExtensions {

        /// <summary>
        /// Filters elements from a source collection into a result collection without allocations.
        /// The result collection is cleared before adding filtered elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="source">The source collection to filter</param>
        /// <param name="predicate">The filter predicate function</param>
        /// <param name="result">The result collection to populate (will be cleared first)</param>
        public static void WhereToList<T>(this IList<T> source, Func<T, bool> predicate, IList<T> result) {
            result.Clear();
            for (int i = 0; i < source.Count; i++) {
                if (predicate(source[i])) {
                    result.Add(source[i]);
                }
            }
        }

        /// <summary>
        /// Filters elements from a source collection into a result collection without allocations.
        /// The result collection is cleared before adding filtered elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="source">The source collection to filter</param>
        /// <param name="predicate">The filter predicate function</param>
        /// <param name="result">The result collection to populate (will be cleared first)</param>
        public static void WhereToList<T>(this ICollection<T> source, Func<T, bool> predicate, IList<T> result) {
            result.Clear();
            foreach (var item in source) {
                if (predicate(item)) {
                    result.Add(item);
                }
            }
        }

        /// <summary>
        /// Transforms elements from a source collection into a result collection without allocations.
        /// The result collection is cleared before adding transformed elements.
        /// </summary>
        /// <typeparam name="TSource">The type of source elements</typeparam>
        /// <typeparam name="TResult">The type of result elements</typeparam>
        /// <param name="source">The source collection to transform</param>
        /// <param name="selector">The transformation function</param>
        /// <param name="result">The result collection to populate (will be cleared first)</param>
        public static void SelectToList<TSource, TResult>(this IList<TSource> source, Func<TSource, TResult> selector, IList<TResult> result) {
            result.Clear();
            for (int i = 0; i < source.Count; i++) {
                result.Add(selector(source[i]));
            }
        }

        /// <summary>
        /// Finds the first element that matches the predicate without allocation.
        /// Returns default(T) if no match is found.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="source">The source collection to search</param>
        /// <param name="predicate">The search predicate function</param>
        /// <returns>The first matching element or default(T)</returns>
        public static T FirstOrDefaultFast<T>(this IList<T> source, Func<T, bool> predicate) {
            for (int i = 0; i < source.Count; i++) {
                if (predicate(source[i])) {
                    return source[i];
                }
            }
            return default(T);
        }

        /// <summary>
        /// Finds the first element that matches the predicate without allocation.
        /// Returns default(T) if no match is found.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="source">The source collection to search</param>
        /// <param name="predicate">The search predicate function</param>
        /// <returns>The first matching element or default(T)</returns>
        public static T FirstOrDefaultFast<T>(this ICollection<T> source, Func<T, bool> predicate) {
            foreach (var item in source) {
                if (predicate(item)) {
                    return item;
                }
            }
            return default(T);
        }

        /// <summary>
        /// Counts elements that match the predicate without allocation.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="source">The source collection to count</param>
        /// <param name="predicate">The counting predicate function</param>
        /// <returns>The count of matching elements</returns>
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
        /// Counts elements that match the predicate without allocation.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="source">The source collection to count</param>
        /// <param name="predicate">The counting predicate function</param>
        /// <returns>The count of matching elements</returns>
        public static int CountWhere<T>(this ICollection<T> source, Func<T, bool> predicate) {
            int count = 0;
            foreach (var item in source) {
                if (predicate(item)) {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Checks if any element matches the predicate without allocation.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="source">The source collection to check</param>
        /// <param name="predicate">The matching predicate function</param>
        /// <returns>True if any element matches, false otherwise</returns>
        public static bool AnyFast<T>(this IList<T> source, Func<T, bool> predicate) {
            for (int i = 0; i < source.Count; i++) {
                if (predicate(source[i])) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if any element matches the predicate without allocation.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="source">The source collection to check</param>
        /// <param name="predicate">The matching predicate function</param>
        /// <returns>True if any element matches, false otherwise</returns>
        public static bool AnyFast<T>(this ICollection<T> source, Func<T, bool> predicate) {
            foreach (var item in source) {
                if (predicate(item)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if all elements match the predicate without allocation.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="source">The source collection to check</param>
        /// <param name="predicate">The matching predicate function</param>
        /// <returns>True if all elements match, false otherwise</returns>
        public static bool AllFast<T>(this IList<T> source, Func<T, bool> predicate) {
            for (int i = 0; i < source.Count; i++) {
                if (!predicate(source[i])) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if all elements match the predicate without allocation.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="source">The source collection to check</param>
        /// <param name="predicate">The matching predicate function</param>
        /// <returns>True if all elements match, false otherwise</returns>
        public static bool AllFast<T>(this ICollection<T> source, Func<T, bool> predicate) {
            foreach (var item in source) {
                if (!predicate(item)) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Casts elements to the specified type and adds them to the result collection.
        /// Only elements that can be cast are included. The result collection is cleared first.
        /// </summary>
        /// <typeparam name="TResult">The type to cast to</typeparam>
        /// <param name="source">The source collection to cast</param>
        /// <param name="result">The result collection to populate (will be cleared first)</param>
        public static void CastToList<TResult>(this ICollection<object> source, IList<TResult> result) where TResult : class {
            result.Clear();
            foreach (var item in source) {
                if (item is TResult castItem) {
                    result.Add(castItem);
                }
            }
        }

        /// <summary>
        /// Finds the maximum value in a collection using a selector function without allocation.
        /// </summary>
        /// <typeparam name="TSource">The type of source elements</typeparam>
        /// <typeparam name="TResult">The type of the comparison value</typeparam>
        /// <param name="source">The source collection</param>
        /// <param name="selector">The value selector function</param>
        /// <returns>The maximum value</returns>
        public static TResult MaxFast<TSource, TResult>(this IList<TSource> source, Func<TSource, TResult> selector) where TResult : IComparable<TResult> {
            if (source.Count == 0) {
                throw new InvalidOperationException("Sequence contains no elements");
            }

            TResult max = selector(source[0]);
            for (int i = 1; i < source.Count; i++) {
                TResult current = selector(source[i]);
                if (current.CompareTo(max) > 0) {
                    max = current;
                }
            }
            return max;
        }

        /// <summary>
        /// Finds the minimum value in a collection using a selector function without allocation.
        /// </summary>
        /// <typeparam name="TSource">The type of source elements</typeparam>
        /// <typeparam name="TResult">The type of the comparison value</typeparam>
        /// <param name="source">The source collection</param>
        /// <param name="selector">The value selector function</param>
        /// <returns>The minimum value</returns>
        public static TResult MinFast<TSource, TResult>(this IList<TSource> source, Func<TSource, TResult> selector) where TResult : IComparable<TResult> {
            if (source.Count == 0) {
                throw new InvalidOperationException("Sequence contains no elements");
            }

            TResult min = selector(source[0]);
            for (int i = 1; i < source.Count; i++) {
                TResult current = selector(source[i]);
                if (current.CompareTo(min) < 0) {
                    min = current;
                }
            }
            return min;
        }

    }
}
