using System;
using System.IO;
using System.Threading.Tasks;

namespace Blish_HUD.Content {
    public sealed class DirectoryReader : IDataReader {

        private readonly string _directoryPath;

        public string PhysicalPath => _directoryPath;

        public DirectoryReader(string directoryPath) {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Directory path {directoryPath} not found.");

            _directoryPath = directoryPath;
        }
        
        public IDataReader GetSubPath(string subPath) {
            if (subPath.StartsWith(_directoryPath, StringComparison.OrdinalIgnoreCase))
                return new DirectoryReader(subPath);

            return new DirectoryReader(Path.Combine(_directoryPath, subPath));
        }
        
        public string GetPathRepresentation(string? relativeFilePath = null) {
            return Path.Combine(_directoryPath, relativeFilePath ?? "");
        }
        
        public void LoadOnFileType(Action<Stream, IDataReader> loadFileFunc, string fileExtension = "", IProgress<string>? progress = null) {
            foreach (string filePath in Directory.EnumerateFiles(_directoryPath, $"*{fileExtension}", SearchOption.AllDirectories)) {
                progress?.Report($"Loading {Path.GetFileName(filePath)}");
                var stream = this.GetFileStream(filePath);
                if (stream != null) {
                    loadFileFunc.Invoke(stream, this);
                }
            }
        }
        
        public bool FileExists(string filePath) {
            return File.Exists(Path.Combine(_directoryPath, filePath));
        }
        
        public Stream? GetFileStream(string filePath) {
            if (!this.FileExists(filePath)) return null;

            return File.Open(Path.Combine(_directoryPath, filePath), FileMode.Open);
        }
        
        public byte[]? GetFileBytes(string filePath) {
            if (!this.FileExists(filePath)) return null;

            return File.ReadAllBytes(Path.Combine(_directoryPath, filePath));
        }
        
        public int GetFileBytes(string filePath, out byte[] fileBuffer) {
            fileBuffer = GetFileBytes(filePath) ?? Array.Empty<byte>();

            return fileBuffer.Length;
        }
        
        public async Task<Stream?> GetFileStreamAsync(string filePath) {
            if (!FileExists(filePath)) return null;

            return await Task.Run(() => File.Open(Path.Combine(_directoryPath, filePath), FileMode.Open, FileAccess.Read, FileShare.Read));
        }
        
        public async Task<byte[]?> GetFileBytesAsync(string filePath) {
            if (!FileExists(filePath)) return null;

            var fullPath = Path.Combine(_directoryPath, filePath);
            
            using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous)) {
                var buffer = new byte[fileStream.Length];
                await fileStream.ReadAsync(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        public void DeleteRoot() {
            this.Dispose();

            Directory.Delete(_directoryPath, true);
        }

        public void Dispose() { /* NOOP */ }

    }
}
