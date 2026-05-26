using Elementary.VerseOfTheDay.Models;
using System.Threading.Tasks;

namespace Elementary.Services
{
    public interface ITileUpdateService
    {
        Task UpdateAsync(VerseOfTheDayResult result);
    }
}
