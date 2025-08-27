using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Blish_HUD._Utils {
    /// <summary>
    /// Provides object pools for commonly used stream and I/O related objects to reduce allocations
    /// and improve performance in data processing, serialization, and I/O operations.
    /// </summary>
    public static class StreamPool {
        
        // Object pools for stream types
        private static readonly ObjectPool<MemoryStream> _memoryStreamPool = new ObjectPool<MemoryStream>(
            objectGenerator: () => new MemoryStream(),
            resetAction: stream => {
                stream.SetLength(0);
                stream.Position = 0;
            },
            validateAction: stream => stream != null && stream.CanWrite,
            maxSize: 50
        );

        // Note: BinaryReader, BinaryWriter, and StringReader cannot be pooled directly
        // because they don't have parameterless constructors and require specific streams.
        // We provide factory methods for these but don't pool the instances themselves.

        private static readonly ObjectPool<StringWriter> _stringWriterPool = new ObjectPool<StringWriter>(
            objectGenerator: () => new StringWriter(),
            resetAction: writer => {
                writer.GetStringBuilder().Clear();
            },
            validateAction: writer => writer != null,
            maxSize: 20
        );

        // Performance tracking
        private static long _memoryStreamRequests = 0;
        private static long _binaryReaderRequests = 0;
        private static long _binaryWriterRequests = 0;
        private static long _stringReaderRequests = 0;
        private static long _stringWriterRequests = 0;

        #region MemoryStream Pool

        /// <summary>
        /// Gets a MemoryStream from the pool.
        /// </summary>
        /// <returns>A MemoryStream instance ready for use.</returns>
        public static MemoryStream GetMemoryStream() {
            Interlocked.Increment(ref _memoryStreamRequests);
            return _memoryStreamPool.Get();
        }

        /// <summary>
        /// Gets a MemoryStream with the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity of the MemoryStream.</param>
        /// <returns>A MemoryStream with the specified capacity.</returns>
        public static MemoryStream GetMemoryStream(int capacity) {
            var stream = GetMemoryStream();
            if (stream.Capacity < capacity) {
                stream.Capacity = capacity;
            }
            return stream;
        }

        /// <summary>
        /// Gets a MemoryStream initialized with the specified data.
        /// </summary>
        /// <param name="data">The data to initialize the MemoryStream with.</param>
        /// <returns>A MemoryStream containing the specified data.</returns>
        public static MemoryStream GetMemoryStream(byte[] data) {
            var stream = GetMemoryStream();
            if (data != null && data.Length > 0) {
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
            }
            return stream;
        }

        /// <summary>
        /// Returns a MemoryStream to the pool for reuse.
        /// </summary>
        /// <param name="stream">The MemoryStream to return to the pool.</param>
        public static void ReturnMemoryStream(MemoryStream stream) {
            if (stream != null) {
                _memoryStreamPool.Return(stream);
            }
        }

        #endregion

        #region BinaryReader Factory

        /// <summary>
        /// Creates a BinaryReader with a pooled MemoryStream.
        /// Note: BinaryReader cannot be pooled directly, but we can reuse the underlying MemoryStream.
        /// </summary>
        /// <returns>A BinaryReader instance with a pooled MemoryStream.</returns>
        public static BinaryReader GetBinaryReader() {
            Interlocked.Increment(ref _binaryReaderRequests);
            var stream = GetMemoryStream();
            return new BinaryReader(stream);
        }

        /// <summary>
        /// Creates a BinaryReader for the specified stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>A BinaryReader for the specified stream.</returns>
        public static BinaryReader GetBinaryReader(Stream stream) {
            Interlocked.Increment(ref _binaryReaderRequests);
            return new BinaryReader(stream);
        }

        /// <summary>
        /// Creates a BinaryReader initialized with the specified data using a pooled MemoryStream.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <returns>A BinaryReader for the specified data.</returns>
        public static BinaryReader GetBinaryReader(byte[] data) {
            Interlocked.Increment(ref _binaryReaderRequests);
            var stream = GetMemoryStream(data);
            return new BinaryReader(stream);
        }

        /// <summary>
        /// Disposes a BinaryReader and returns its underlying MemoryStream to the pool if applicable.
        /// </summary>
        /// <param name="reader">The BinaryReader to dispose.</param>
        public static void ReturnBinaryReader(BinaryReader reader) {
            if (reader != null) {
                if (reader.BaseStream is MemoryStream ms) {
                    reader.Dispose(); // This will dispose the reader but not the stream
                    ReturnMemoryStream(ms);
                } else {
                    reader.Dispose();
                }
            }
        }

        #endregion

        #region BinaryWriter Factory

        /// <summary>
        /// Creates a BinaryWriter with a pooled MemoryStream.
        /// Note: BinaryWriter cannot be pooled directly, but we can reuse the underlying MemoryStream.
        /// </summary>
        /// <returns>A BinaryWriter instance with a pooled MemoryStream.</returns>
        public static BinaryWriter GetBinaryWriter() {
            Interlocked.Increment(ref _binaryWriterRequests);
            var stream = GetMemoryStream();
            return new BinaryWriter(stream);
        }

        /// <summary>
        /// Creates a BinaryWriter for the specified stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <returns>A BinaryWriter for the specified stream.</returns>
        public static BinaryWriter GetBinaryWriter(Stream stream) {
            Interlocked.Increment(ref _binaryWriterRequests);
            return new BinaryWriter(stream);
        }

        /// <summary>
        /// Disposes a BinaryWriter and returns its underlying MemoryStream to the pool if applicable.
        /// </summary>
        /// <param name="writer">The BinaryWriter to dispose.</param>
        public static void ReturnBinaryWriter(BinaryWriter writer) {
            if (writer != null) {
                if (writer.BaseStream is MemoryStream ms) {
                    writer.Flush();
                    writer.Dispose(); // This will dispose the writer but not the stream
                    ReturnMemoryStream(ms);
                } else {
                    writer.Dispose();
                }
            }
        }

        #endregion

        #region StringReader Factory

        /// <summary>
        /// Creates a StringReader initialized with the specified text.
        /// Note: StringReader cannot be pooled because it cannot be reset.
        /// </summary>
        /// <param name="text">The text to read from.</param>
        /// <returns>A StringReader for the specified text.</returns>
        public static StringReader GetStringReader(string text) {
            Interlocked.Increment(ref _stringReaderRequests);
            return new StringReader(text ?? string.Empty);
        }

        /// <summary>
        /// Disposes a StringReader. Since StringReader cannot be pooled, this simply disposes it.
        /// </summary>
        /// <param name="reader">The StringReader to dispose.</param>
        public static void ReturnStringReader(StringReader reader) {
            reader?.Dispose();
        }

        #endregion

        #region StringWriter Pool

        /// <summary>
        /// Gets a StringWriter from the pool.
        /// </summary>
        /// <returns>A StringWriter instance ready for use.</returns>
        public static StringWriter GetStringWriter() {
            Interlocked.Increment(ref _stringWriterRequests);
            return _stringWriterPool.Get();
        }

        /// <summary>
        /// Gets a StringWriter with the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity of the underlying StringBuilder.</param>
        /// <returns>A StringWriter with the specified capacity.</returns>
        public static StringWriter GetStringWriter(int capacity) {
            var writer = GetStringWriter();
            var sb = writer.GetStringBuilder();
            if (sb.Capacity < capacity) {
                sb.Capacity = capacity;
            }
            return writer;
        }

        /// <summary>
        /// Returns a StringWriter to the pool for reuse.
        /// </summary>
        /// <param name="writer">The StringWriter to return to the pool.</param>
        public static void ReturnStringWriter(StringWriter writer) {
            if (writer != null) {
                _stringWriterPool.Return(writer);
            }
        }

        #endregion

        #region Pool Statistics and Management

        /// <summary>
        /// Gets performance statistics for all stream pools.
        /// </summary>
        /// <returns>A formatted string containing performance statistics for all stream pools.</returns>
        public static string GetPerformanceStatistics() {
            var stats = "Stream Pool Statistics:\n";
            stats += $"  MemoryStream Pool - {_memoryStreamPool.GetPerformanceStatistics()}\n";
            stats += $"  BinaryReader Factory - Not pooled (creates new instances with pooled MemoryStreams)\n";
            stats += $"  BinaryWriter Factory - Not pooled (creates new instances with pooled MemoryStreams)\n";
            stats += $"  StringReader Factory - Not pooled (always creates new instances)\n";
            stats += $"  StringWriter Pool - {_stringWriterPool.GetPerformanceStatistics()}\n";
            stats += $"  Total Stream Requests: MemoryStream={_memoryStreamRequests}, BinaryReader={_binaryReaderRequests}, " +
                    $"BinaryWriter={_binaryWriterRequests}, StringReader={_stringReaderRequests}, StringWriter={_stringWriterRequests}";
            
            return stats;
        }

        /// <summary>
        /// Resets performance counters for all stream pools.
        /// </summary>
        public static void ResetPerformanceCounters() {
            _memoryStreamPool.ResetPerformanceCounters();
            _stringWriterPool.ResetPerformanceCounters();
            
            Interlocked.Exchange(ref _memoryStreamRequests, 0);
            Interlocked.Exchange(ref _binaryReaderRequests, 0);
            Interlocked.Exchange(ref _binaryWriterRequests, 0);
            Interlocked.Exchange(ref _stringReaderRequests, 0);
            Interlocked.Exchange(ref _stringWriterRequests, 0);
        }

        /// <summary>
        /// Gets the total number of objects across all stream pools.
        /// </summary>
        public static int TotalPoolSize => _memoryStreamPool.PoolSize + _stringWriterPool.PoolSize;

        /// <summary>
        /// Gets the combined hit rate across all stream pools.
        /// </summary>
        public static double AverageHitRate {
            get {
                var pools = new ObjectPool<object>[] { 
                    (ObjectPool<object>)(object)_memoryStreamPool, 
                    (ObjectPool<object>)(object)_stringWriterPool 
                };
                double totalHitRate = 0;
                int poolCount = 0;

                foreach (var pool in pools) {
                    if (pool.HitRate > 0) {
                        totalHitRate += pool.HitRate;
                        poolCount++;
                    }
                }

                return poolCount > 0 ? totalHitRate / poolCount : 0;
            }
        }

        /// <summary>
        /// Clears all stream pools. Use with caution.
        /// </summary>
        public static void ClearAllPools() {
            _memoryStreamPool.Clear();
            _stringWriterPool.Clear();
        }

        #endregion
    }

    #region Extension Methods for Convenient Usage

    /// <summary>
    /// Extension methods for convenient usage of stream pools with automatic return to pool.
    /// </summary>
    public static class StreamPoolExtensions {
        /// <summary>
        /// Gets a pooled MemoryStream wrapped in a PooledObject that automatically returns to pool when disposed.
        /// </summary>
        /// <param name="capacity">The initial capacity of the MemoryStream.</param>
        /// <returns>A PooledObject containing the MemoryStream.</returns>
        public static PooledMemoryStream GetPooledMemoryStream(int capacity = -1) {
            return new PooledMemoryStream(capacity > 0 ? StreamPool.GetMemoryStream(capacity) : StreamPool.GetMemoryStream());
        }

        /// <summary>
        /// Gets a pooled MemoryStream initialized with data, wrapped in a PooledObject that automatically returns to pool when disposed.
        /// </summary>
        /// <param name="data">The data to initialize the MemoryStream with.</param>
        /// <returns>A PooledObject containing the MemoryStream.</returns>
        public static PooledMemoryStream GetPooledMemoryStream(byte[] data) {
            return new PooledMemoryStream(StreamPool.GetMemoryStream(data));
        }

        /// <summary>
        /// Gets a pooled BinaryWriter wrapped in a PooledObject that automatically returns to pool when disposed.
        /// </summary>
        /// <returns>A PooledObject containing the BinaryWriter.</returns>
        public static PooledBinaryWriter GetPooledBinaryWriter() {
            return new PooledBinaryWriter(StreamPool.GetBinaryWriter());
        }

        /// <summary>
        /// Gets a pooled StringWriter wrapped in a PooledObject that automatically returns to pool when disposed.
        /// </summary>
        /// <param name="capacity">The initial capacity of the underlying StringBuilder.</param>
        /// <returns>A PooledObject containing the StringWriter.</returns>
        public static PooledStringWriter GetPooledStringWriter(int capacity = -1) {
            return new PooledStringWriter(capacity > 0 ? StreamPool.GetStringWriter(capacity) : StreamPool.GetStringWriter());
        }
    }

    /// <summary>
    /// Wrapper for pooled MemoryStream that automatically returns to pool when disposed.
    /// </summary>
    public struct PooledMemoryStream : IDisposable {
        private readonly MemoryStream _stream;
        private bool _disposed;

        internal PooledMemoryStream(MemoryStream stream) {
            _stream = stream;
            _disposed = false;
        }

        public MemoryStream Value => _disposed ? throw new ObjectDisposedException(nameof(PooledMemoryStream)) : _stream;

        public void Dispose() {
            if (!_disposed && _stream != null) {
                StreamPool.ReturnMemoryStream(_stream);
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Wrapper for pooled BinaryWriter that automatically returns to pool when disposed.
    /// </summary>
    public struct PooledBinaryWriter : IDisposable {
        private readonly BinaryWriter _writer;
        private bool _disposed;

        internal PooledBinaryWriter(BinaryWriter writer) {
            _writer = writer;
            _disposed = false;
        }

        public BinaryWriter Value => _disposed ? throw new ObjectDisposedException(nameof(PooledBinaryWriter)) : _writer;

        public void Dispose() {
            if (!_disposed && _writer != null) {
                StreamPool.ReturnBinaryWriter(_writer);
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Wrapper for pooled StringWriter that automatically returns to pool when disposed.
    /// </summary>
    public struct PooledStringWriter : IDisposable {
        private readonly StringWriter _writer;
        private bool _disposed;

        internal PooledStringWriter(StringWriter writer) {
            _writer = writer;
            _disposed = false;
        }

        public StringWriter Value => _disposed ? throw new ObjectDisposedException(nameof(PooledStringWriter)) : _writer;

        public void Dispose() {
            if (!_disposed && _writer != null) {
                StreamPool.ReturnStringWriter(_writer);
                _disposed = true;
            }
        }
    }

    #endregion
}
