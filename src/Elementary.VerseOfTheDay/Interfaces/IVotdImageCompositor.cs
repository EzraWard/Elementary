using Elementary.VerseOfTheDay.Models;

namespace Elementary.VerseOfTheDay.Interfaces
{
    public interface IVotdImageCompositor
    {
        byte[] Compose(BibleVerseData verse, VotdImageSize size, string seed);
    }
}
