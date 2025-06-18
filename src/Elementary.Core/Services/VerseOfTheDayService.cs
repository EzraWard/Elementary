using Elementary.Models;
using Elementary.Services;
using System;

namespace Elementary.Services
{
    public class VerseOfTheDayService : IVerseOfTheDayService
    {
        private const string BaseImageUrl = "https://votd.olivetree.com/";
        private const string ImageFormat = "NKJV.jpg";

        public VerseOfTheDay GetVerseOfTheDay(DateTime? date = null)
        {
            var targetDate = date ?? DateTime.Now;
            var month = targetDate.ToString("MM");
            var day = targetDate.ToString("dd");
            var imageUrl = $"{BaseImageUrl}{month}_{day}_{ImageFormat}";

            return new VerseOfTheDay
            {
                Date = targetDate,
                ImageUrl = imageUrl
            };
        }
    }
}