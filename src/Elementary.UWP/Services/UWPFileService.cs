using Elementary.Core.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Elementary.Services
{
    internal class UWPFileService : IFileService
    {
        public Stream ReadFile(string path)
        {
            // StorageFile APIs are async, so we can't do truly sync read.
            // We'll block here for compatibility, but you should prefer async loading elsewhere!
            return ReadFileAsync(path).GetAwaiter().GetResult();
        }

        private async Task<Stream> ReadFileAsync(string path)
        {
            StorageFile file;

            if (path.StartsWith("ms-appx:///"))
            {
                var uri = new Uri(path);
                file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            }
            else
            {
                // Assume local storage
                file = await StorageFile.GetFileFromPathAsync(path);
            }

            return await file.OpenStreamForReadAsync();
        }

        public async Task WriteFileAsync(string path, Stream content)
        {
            // Assume local storage only for writing
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);

            using (var outputStream = await file.OpenStreamForWriteAsync())
            {
                await content.CopyToAsync(outputStream);
            }
        }

        public async Task<bool> FileExistsAsync(string path)
        {
            try
            {
                if (path.StartsWith("ms-appx:///"))
                {
                    var uri = new Uri(path);
                    await StorageFile.GetFileFromApplicationUriAsync(uri);
                }
                else
                {
                    await StorageFile.GetFileFromPathAsync(path);
                }

                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}