using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Elementary.Core.Interfaces
{
    public interface IFileService
    {
        Task<Stream> ReadFileAsync(string path);

        Task WriteFileAsync(string path, Stream content);

        Task<bool> FileExistsAsync(string path);

        // Lists files under the given path. For file-system paths this is a directory; for ms-appx URIs this should be a folder URI.
        Task<IEnumerable<string>> ListFilesAsync(string path, string searchPattern = "*");
    }
}