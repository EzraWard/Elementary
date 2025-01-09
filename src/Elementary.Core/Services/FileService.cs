using Elementary.Core.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace Elementary.Core.Services
{
    public class FileService: IFileService
    {
        public Stream ReadFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"File not found: {path}");

            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
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
