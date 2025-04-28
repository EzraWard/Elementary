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
    }
}