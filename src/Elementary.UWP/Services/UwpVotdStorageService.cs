#nullable enable
using Elementary.VerseOfTheDay.Interfaces;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace Elementary.Services
{
    public class UwpVotdStorageService : IVotdStorageService
    {
        private const string CacheFolderName = "votd_cache";
 
        private StorageFolder? _cacheFolder;

        private async Task<StorageFolder> GetCacheFolderAsync()
        {
            if (_cacheFolder != null) return _cacheFolder;
            _cacheFolder = await ApplicationData.Current.LocalFolder
                .CreateFolderAsync(CacheFolderName, CreationCollisionOption.OpenIfExists);
            return _cacheFolder;
        }

        public async Task SaveAsync(string filename, byte[] data)
        {
            var folder = await GetCacheFolderAsync().ConfigureAwait(false);
            var file = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteBytesAsync(file, data);
        }

        public async Task<byte[]?> LoadAsync(string filename)
        {
            try
            {
                var folder = await GetCacheFolderAsync().ConfigureAwait(false);
                var file = await folder.GetFileAsync(filename);
                var buffer = await FileIO.ReadBufferAsync(file);
                var bytes = new byte[buffer.Length];
                Windows.Storage.Streams.DataReader.FromBuffer(buffer).ReadBytes(bytes);
                return bytes;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> ExistsAsync(string filename)
        {
            try
            {
                var folder = await GetCacheFolderAsync().ConfigureAwait(false);
                await folder.GetFileAsync(filename);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the ms-appdata URI for a cached file, usable in tile XML and XAML image sources.
        /// </summary>
        public static string GetMsAppDataUri(string filename)
            => $"ms-appdata:///local/{CacheFolderName}/{filename}";
    }
}
