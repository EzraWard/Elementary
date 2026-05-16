using Elementary.VerseOfTheDay.Models;
using System.Threading.Tasks;

namespace Elementary.VerseOfTheDay.Interfaces
{
    public interface IVerseOfTheDayService
    {
        Task<VerseOfTheDayResult> GetAsync(VotdImageSize size);
    }
}
