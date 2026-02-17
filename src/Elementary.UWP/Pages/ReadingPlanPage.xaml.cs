using Elementary.Core.Models;
using Elementary.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Elementary
{
    public sealed partial class ReadingPlanPage : Page
    {
        private List<Chapter> _todaysChapters = new List<Chapter>();

        public ReadingPlanPage()
        {
            this.InitializeComponent();
            Loaded += ReadingPlanPage_Loaded;
        }

        private async void ReadingPlanPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadPlanAsync();
        }

        private async Task LoadPlanAsync()
        {
            try
            {
                var bibleService = App.Services.GetRequiredService<IBibleService>();
                var bible = await bibleService.GetBible(Core.Enums.ETranslation.NET);
                var flat = new List<Chapter>();
                foreach (var book in bible.Books)
                {
                    foreach (var chap in book.Chapters)
                    {
                        flat.Add(chap);
                    }
                }

                if (flat.Count == 0)
                {
                    ChaptersListView.ItemsSource = new[] { "No bible content available" };
                    return;
                }

                int total = flat.Count;
                int dayOfYear = DateTime.Now.DayOfYear;
                int chunkSize = (int)Math.Ceiling(total / 365.0);
                int start = ((dayOfYear - 1) * chunkSize) % total;

                _todaysChapters.Clear();
                for (int i = 0; i < chunkSize; i++)
                {
                    _todaysChapters.Add(flat[(start + i) % total]);
                }

                ChaptersListView.ItemsSource = _todaysChapters.Select(c =>
                {
                    var book = bible.Books.FirstOrDefault(b => b.Chapters.Contains(c));
                    return $"{book?.Title} {c.Index}";
                }).ToList();
            }
            catch
            {
                ChaptersListView.ItemsSource = new[] { "Failed to load reading plan" };
            }
        }

        private void ChaptersListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_todaysChapters == null || _todaysChapters.Count == 0) return;

            // Navigate the parent frame (this.Frame is the ContentFrame hosted by MainPage)
            if (this.Frame == null) return;

            this.Frame.Navigate(typeof(BiblePage));
            this.Frame.Navigated += Frame_Navigated;
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            this.Frame.Navigated -= Frame_Navigated;
            if (e.Content is BiblePage biblePage)
            {
                if (!biblePage._isLoaded)
                {
                    biblePage.Loaded += (s, ev) => biblePage.ShowChapters(_todaysChapters);
                }
                else
                {
                    biblePage.ShowChapters(_todaysChapters);
                }
            }
        }
    }
}