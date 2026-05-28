using Elementary.Core.Dictionaries;
using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elementary.Core.Services
{
    public class ReadingPlanService : IReadingPlanService
    {
        private static readonly EBook[] NewTestamentBooks =
        {
            EBook.Matthew,
            EBook.Mark,
            EBook.Luke,
            EBook.John,
            EBook.Acts,
            EBook.Romans,
            EBook.FirstCorinthians,
            EBook.SecondCorinthians,
            EBook.Galatians,
            EBook.Ephesians,
            EBook.Philippians,
            EBook.Colossians,
            EBook.FirstThessalonians,
            EBook.SecondThessalonians,
            EBook.FirstTimothy,
            EBook.SecondTimothy,
            EBook.Titus,
            EBook.Philemon,
            EBook.Hebrews,
            EBook.James,
            EBook.FirstPeter,
            EBook.SecondPeter,
            EBook.FirstJohn,
            EBook.SecondJohn,
            EBook.ThirdJohn,
            EBook.Jude,
            EBook.Revelation
        };

        private readonly ISettingsService _settingsService;
        private readonly List<ReadingPlan> _builtInPlans;

        public ReadingPlanService(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _builtInPlans = CreateBuiltInPlans();
        }

        public IReadOnlyList<ReadingPlan> GetBuiltInPlans()
        {
            return _builtInPlans;
        }

        public ReadingPlanProgress GetProgress()
        {
            return _settingsService.GetReadingPlanProgress() ?? new ReadingPlanProgress();
        }

        public ReadingPlan GetActivePlan()
        {
            var progress = GetProgress();
            if (!progress.HasActivePlan)
            {
                return null;
            }

            return GetPlanById(progress.ActivePlanId);
        }

        public ReadingPlanDay GetCurrentDay()
        {
            var progress = GetProgress();
            var activePlan = GetActivePlan();
            if (activePlan == null)
            {
                return null;
            }

            var completedDayCount = Math.Max(0, progress.CompletedDayCount);
            if (completedDayCount >= activePlan.TotalDays)
            {
                return null;
            }

            return activePlan.Days[completedDayCount];
        }

        public void StartPlan(string planId)
        {
            var plan = GetPlanById(planId);
            if (plan == null)
            {
                return;
            }

            _settingsService.SaveReadingPlanProgress(new ReadingPlanProgress
            {
                ActivePlanId = plan.Id,
                CompletedDayCount = 0
            });
        }

        public void ClearActivePlan()
        {
            _settingsService.SaveReadingPlanProgress(new ReadingPlanProgress());
        }

        public bool CompleteCurrentDay()
        {
            var progress = GetProgress();
            var activePlan = GetActivePlan();
            if (activePlan == null)
            {
                return false;
            }

            var completedDayCount = Math.Max(0, progress.CompletedDayCount);
            if (completedDayCount >= activePlan.TotalDays)
            {
                return false;
            }

            _settingsService.SaveReadingPlanProgress(new ReadingPlanProgress
            {
                ActivePlanId = activePlan.Id,
                CompletedDayCount = completedDayCount + 1
            });

            return true;
        }

        private ReadingPlan GetPlanById(string planId)
        {
            if (string.IsNullOrWhiteSpace(planId))
            {
                return null;
            }

            return _builtInPlans.FirstOrDefault(plan => string.Equals(plan.Id, planId, StringComparison.OrdinalIgnoreCase));
        }
        private static List<ReadingPlan> CreateBuiltInPlans()
        {
            return new List<ReadingPlan>
            {
                CreateSingleBookPlan(
                    "john-in-21-days",
                    "21 Days in John",
                    "Read one chapter of John each day."),
                CreateSingleBookPlan(
                    "proverbs-in-31-days",
                    "31 Days in Proverbs",
                    "Read one chapter of Proverbs each day for a month of wisdom.",
                    EBook.Proverbs),
                CreateSequentialBooksPlan(
                    "new-testament-in-260-days",
                    "New Testament in 260 Days",
                    "Read through the entire New Testament one chapter at a time.",
                    NewTestamentBooks)
            };
        }

        private static ReadingPlan CreateSingleBookPlan(string id, string title, string description)
        {
            return CreateSingleBookPlan(id, title, description, EBook.John);
        }

        private static ReadingPlan CreateSingleBookPlan(string id, string title, string description, EBook book)
        {
            return CreateSequentialBooksPlan(id, title, description, new[] { book });
        }

        private static ReadingPlan CreateSequentialBooksPlan(string id, string title, string description, IReadOnlyList<EBook> books)
        {
            var days = new List<ReadingPlanDay>();
            var dayNumber = 1;

            foreach (var book in books)
            {
                if (!EBookToLocation.EBookToChapterCount.TryGetValue(book, out var chapterCount))
                {
                    continue;
                }

                for (var chapter = 1; chapter <= chapterCount; chapter++)
                {
                    days.Add(new ReadingPlanDay
                    {
                        DayNumber = dayNumber++,
                        Passages = new List<ReadingPlanPassage>
                        {
                            new ReadingPlanPassage
                            {
                                Book = book,
                                Chapter = chapter
                            }
                        }
                    });
                }
            }

            return new ReadingPlan
            {
                Id = id,
                Title = title,
                Description = description,
                Days = days
            };
        }
    }
}
