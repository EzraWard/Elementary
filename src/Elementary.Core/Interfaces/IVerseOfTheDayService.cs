using Elementary.Models;
using System;

namespace Elementary.Services
{
    public interface IVerseOfTheDayService
    {
        VerseOfTheDay GetVerseOfTheDay(DateTime? date = null);
    }
}