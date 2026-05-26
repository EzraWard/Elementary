using System.Threading.Tasks;

namespace Elementary.VerseOfTheDay.Interfaces
{
    public interface IVotdStorageService
    {
        Task SaveAsync(string filename, byte[] data);
        Task<byte[]?> LoadAsync(string filename);
        Task<bool> ExistsAsync(string filename);
    }
}
