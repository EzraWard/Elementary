using Elementary.Core.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace Elementary.Core.Services
{
    public class FileService: IFileService
    {
        public async Task<Stream> ReadFileAsync(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"File not found at path: {path}");
            }

            var memoryStream = new MemoryStream();
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await fileStream.CopyToAsync(memoryStream);
            }

            memoryStream.Position = 0; // Reset stream position to the beginning
            return memoryStream;
        }

        public async Task WriteFileAsync(string path, Stream content)
        {
            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await content.CopyToAsync(fileStream);
            }
        }

        public Task<bool> FileExistsAsync(string path)
        {
            return Task.FromResult(File.Exists(path));
        }
    }
}
