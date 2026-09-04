using Elementary.Core.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.Storage;
using Windows.ApplicationModel;

namespace Elementary.Services
{
    internal class UWPFileService : IFileService
    {
        public async Task<Stream> ReadFileAsync(string path)
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

        public async Task<IEnumerable<string>> ListFilesAsync(string path, string searchPattern = "*")
        {
            try
            {
                if (path.StartsWith("ms-appx:///"))
                {
                    try
                    {
                        // Convert ms-appx:///Content/NET to relative path "Content/NET"
                        var uri = new Uri(path);
                        var rel = uri.AbsolutePath.TrimStart('/'); // e.g., "Content/NET"
                        var installed = Package.Current.InstalledLocation;

                        StorageFolder folder = installed;
                        var parts = rel.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var part in parts)
                        {
                            folder = await folder.GetFolderAsync(part);
                        }

                        var files = await folder.GetFilesAsync();
                        var filtered = new List<string>();
                        foreach (var file in files)
                        {
                            if (System.Text.RegularExpressions.Regex.IsMatch(file.Name, GlobToRegex(searchPattern), System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            {
                                var baseUri = path.TrimEnd('/');
                                filtered.Add(baseUri + "/" + file.Name);
                            }
                        }

                        return filtered;
                    }
                    catch
                    {
                        return new string[0];
                    }
                }
                else
                {
                    if (!System.IO.Directory.Exists(path))
                    {
                        return new string[0];
                    }

                    var files = System.IO.Directory.GetFiles(path, searchPattern);
                    return files;
                }
            }
            catch
            {
                return new string[0];
            }
        }

        private string GlobToRegex(string glob)
        {
            // Very small glob to regex converter for simple patterns like *.usfm
            string escaped = System.Text.RegularExpressions.Regex.Escape(glob).Replace("\\*", ".*").Replace("\\?", ".");
            return "^" + escaped + "$";
        }
    }
}