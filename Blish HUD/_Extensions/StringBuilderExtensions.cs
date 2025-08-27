using System;
using System.Text;
using Blish_HUD._Utils;

namespace Blish_HUD._Extensions {
    /// <summary>
    /// Extension methods for StringBuilder to provide convenient pooling operations.
    /// </summary>
    public static class StringBuilderExtensions {
        /// <summary>
        /// Appends a line separator followed by a formatted string to the StringBuilder.
        /// </summary>
        /// <param name="sb">The StringBuilder to append to.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        /// <returns>The StringBuilder instance for method chaining.</returns>
        public static StringBuilder AppendLineFormat(this StringBuilder sb, string format, params object[] args) {
            return sb.AppendLine(string.Format(format, args));
        }

        /// <summary>
        /// Appends a string conditionally based on a boolean condition.
        /// </summary>
        /// <param name="sb">The StringBuilder to append to.</param>
        /// <param name="condition">The condition to check.</param>
        /// <param name="value">The string to append if condition is true.</param>
        /// <returns>The StringBuilder instance for method chaining.</returns>
        public static StringBuilder AppendIf(this StringBuilder sb, bool condition, string value) {
            if (condition) {
                sb.Append(value);
            }
            return sb;
        }

        /// <summary>
        /// Appends a formatted string conditionally based on a boolean condition.
        /// </summary>
        /// <param name="sb">The StringBuilder to append to.</param>
        /// <param name="condition">The condition to check.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        /// <returns>The StringBuilder instance for method chaining.</returns>
        public static StringBuilder AppendFormatIf(this StringBuilder sb, bool condition, string format, params object[] args) {
            if (condition) {
                sb.AppendFormat(format, args);
            }
            return sb;
        }

        /// <summary>
        /// Appends a separator string if the StringBuilder is not empty.
        /// Useful for building comma-separated lists or similar constructs.
        /// </summary>
        /// <param name="sb">The StringBuilder to append to.</param>
        /// <param name="separator">The separator string to append.</param>
        /// <returns>The StringBuilder instance for method chaining.</returns>
        public static StringBuilder AppendSeparatorIfNotEmpty(this StringBuilder sb, string separator) {
            if (sb.Length > 0) {
                sb.Append(separator);
            }
            return sb;
        }

        /// <summary>
        /// Returns the StringBuilder to the pool and gets the final string result.
        /// This is a convenience method that combines ToString() and Return() operations.
        /// </summary>
        /// <param name="sb">The StringBuilder to return to the pool.</param>
        /// <returns>The final string content of the StringBuilder.</returns>
        public static string ToStringAndReturn(this StringBuilder sb) {
            try {
                return sb.ToString();
            } finally {
                StringBuilderPool.Return(sb);
            }
        }
    }

    /// <summary>
    /// Utility class for common string building patterns using pooled StringBuilders.
    /// </summary>
    public static class PooledStringBuilder {
        /// <summary>
        /// Creates a formatted string using a pooled StringBuilder.
        /// This is more efficient than string.Format for complex formatting operations.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        /// <returns>The formatted string.</returns>
        public static string Format(string format, params object[] args) {
            var sb = StringBuilderPool.Get();
            try {
                sb.AppendFormat(format, args);
                return sb.ToString();
            } finally {
                StringBuilderPool.Return(sb);
            }
        }

        /// <summary>
        /// Joins a collection of strings with a separator using a pooled StringBuilder.
        /// This is more efficient than string.Join for large collections.
        /// </summary>
        /// <param name="separator">The separator string.</param>
        /// <param name="values">The strings to join.</param>
        /// <returns>The joined string.</returns>
        public static string Join(string separator, params string[] values) {
            if (values == null || values.Length == 0) return string.Empty;
            if (values.Length == 1) return values[0] ?? string.Empty;

            var sb = StringBuilderPool.Get();
            try {
                sb.Append(values[0]);
                for (int i = 1; i < values.Length; i++) {
                    sb.Append(separator);
                    sb.Append(values[i]);
                }
                return sb.ToString();
            } finally {
                StringBuilderPool.Return(sb);
            }
        }

        /// <summary>
        /// Concatenates multiple strings using a pooled StringBuilder.
        /// This is more efficient than string concatenation for multiple strings.
        /// </summary>
        /// <param name="values">The strings to concatenate.</param>
        /// <returns>The concatenated string.</returns>
        public static string Concat(params string[] values) {
            if (values == null || values.Length == 0) return string.Empty;
            if (values.Length == 1) return values[0] ?? string.Empty;

            var sb = StringBuilderPool.Get();
            try {
                foreach (var value in values) {
                    sb.Append(value);
                }
                return sb.ToString();
            } finally {
                StringBuilderPool.Return(sb);
            }
        }
    }
}
