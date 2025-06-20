using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Elementary.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel;
using Windows.UI.Xaml;

namespace Elementary.ViewModels
{
    public class SettingsPageViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _settingsService;
        private AppSettings _settings;

        public SettingsPageViewModel()
        {
            _settingsService = App.Services.GetRequiredService<ISettingsService>();
            LoadSettings();
        }

        public string ApplicationVersion => GetApplicationVersion();

        // Translation options
        public List<TranslationOption> TranslationOptions { get; } = new List<TranslationOption>
        {
            new TranslationOption { Display = "NET", Value = ETranslation.NET },
            new TranslationOption { Display = "ASV", Value = ETranslation.ASV },
            new TranslationOption { Display = "KJV", Value = ETranslation.KJV }
        };

        // Font options
        public List<FontOption> FontOptions { get; } = new List<FontOption>
        {
            new FontOption { Display = "Segoe UI", Value = EFont.SegoeUIVariable },
            new FontOption { Display = "Georgia", Value = EFont.Georgia }
        };

        // Font size options
        public List<FontSizeOption> FontSizeOptions { get; } = new List<FontSizeOption>
        {
            new FontSizeOption { Display = "Small", Value = EFontSize.Small },
            new FontSizeOption { Display = "Medium", Value = EFontSize.Medium },
            new FontSizeOption { Display = "Large", Value = EFontSize.Large }
        };

        // Theme options
        public List<ThemeOption> ThemeOptions { get; } = new List<ThemeOption>
        {
            new ThemeOption { Display = "System", Value = ETheme.System },
            new ThemeOption { Display = "Light", Value = ETheme.Light },
            new ThemeOption { Display = "Dark", Value = ETheme.Dark }
        };

        // Settings properties
        public TranslationOption SelectedTranslation
        {
            get => TranslationOptions.FirstOrDefault(t => t.Value == _settings.Translation);
            set
            {
                if (value != null && _settings.Translation != value.Value)
                {
                    _settings.Translation = value.Value;
                    _settings.Book = EBook.Genesis;
                    _settings.Chapter = 1;
                    SaveSettings();
                    OnPropertyChanged();
                }
            }
        }

        public FontOption SelectedFont
        {
            get => FontOptions.FirstOrDefault(f => f.Value == _settings.Font);
            set
            {
                if (value != null && _settings.Font != value.Value)
                {
                    _settings.Font = value.Value;
                    SaveSettings();
                    OnPropertyChanged();
                }
            }
        }

        public FontSizeOption SelectedFontSize
        {
            get => FontSizeOptions.FirstOrDefault(fs => fs.Value == _settings.FontSize);
            set
            {
                if (value != null && _settings.FontSize != value.Value)
                {
                    _settings.FontSize = value.Value;
                    SaveSettings();
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowVerseNumbers
        {
            get => _settings.ShowVerseNumbers ?? true;
            set
            {
                if (_settings.ShowVerseNumbers != value)
                {
                    _settings.ShowVerseNumbers = value;
                    SaveSettings();
                    OnPropertyChanged();
                }
            }
        }

        public ThemeOption SelectedTheme
        {
            get => ThemeOptions.FirstOrDefault(t => t.Value == _settings.Theme);
            set
            {
                if (value != null && _settings.Theme != value.Value)
                {
                    _settings.Theme = value.Value;
                    SaveSettings();
                    ApplyTheme(value.Value);
                    OnPropertyChanged();
                }
            }
        }

        private void LoadSettings()
        {
            _settings = (AppSettings)_settingsService.GetSettings();
        }

        private void SaveSettings()
        {
            _settingsService.SaveSettings(_settings);
        }

        private void ApplyTheme(ETheme theme)
        {
            ApplicationTheme applicationTheme = ApplicationTheme.Light;

            if (Window.Current.Content is FrameworkElement frameworkElement)
            {
                switch (theme)
                {
                    case ETheme.Dark:
                        frameworkElement.RequestedTheme = ElementTheme.Dark;
                        applicationTheme = ApplicationTheme.Dark;
                        break;
                    case ETheme.Light:
                        frameworkElement.RequestedTheme = ElementTheme.Light;
                        applicationTheme = ApplicationTheme.Light;
                        break;
                    case ETheme.System:
                        frameworkElement.RequestedTheme = ElementTheme.Default;
                        var currentAppTheme = ThemeHelpers.GetCurrentApplicationTheme();
                        applicationTheme = currentAppTheme == "Dark" ? ApplicationTheme.Dark : ApplicationTheme.Light;
                        break;
                }
            }

            WindowHelpers.SetCaptionButtonColors(applicationTheme);
        }

        private static string GetApplicationVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Helper classes for ComboBox binding
    public class TranslationOption
    {
        public string Display { get; set; }
        public ETranslation Value { get; set; }
    }

    public class FontOption
    {
        public string Display { get; set; }
        public EFont Value { get; set; }
    }

    public class FontSizeOption
    {
        public string Display { get; set; }
        public EFontSize Value { get; set; }
    }

    public class ThemeOption
    {
        public string Display { get; set; }
        public ETheme Value { get; set; }
    }
}