using Elementary.VerseOfTheDay.Models;
using System.Threading.Tasks;

namespace Elementary.VerseOfTheDay.Interfaces
{
    public interface IUnsplashService
    {
        Task<UnsplashPhoto> FetchAsync();
    }
}
