using Elementary.VerseOfTheDay.Models;

namespace Elementary.VerseOfTheDay.Interfaces
{
    public interface IVotdImageCompositor
    {
        byte[] Compose(UnsplashPhoto photo, BibleVerseData verse, VotdImageSize size);
    }
}
