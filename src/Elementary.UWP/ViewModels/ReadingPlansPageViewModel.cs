using CommunityToolkit.Mvvm.ComponentModel;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Elementary.ViewModels
{
    public class ReadingPlansPageViewModel : ObservableObject
    {
        private readonly IReadingPlanService _readingPlanService;
        private readonly ObservableCollection<ReadingPlanPassage> _visibleDayPassages = new ObservableCollection<ReadingPlanPassage>();
        private ReadingPlan _selectedPlan;
        private ReadingPlanProgress _progress = new ReadingPlanProgress();
        private ReadingPlanDay _visibleDay;

        public ReadingPlansPageViewModel()
        {
            _readingPlanService = App.Services.GetRequiredService<IReadingPlanService>();
        }

        public ObservableCollection<ReadingPlan> Plans { get; } = new ObservableCollection<ReadingPlan>();

        public ObservableCollection<ReadingPlanPassage> VisibleDayPassages => _visibleDayPassages;

        public ReadingPlan SelectedPlan
        {
            get => _selectedPlan;
            set
            {
                if (SetProperty(ref _selectedPlan, value))
                {
                    RefreshComputedState();
                }
            }
        }

        public bool HasSelectedPlan => SelectedPlan != null;

        public bool HasVisibleDay => _visibleDay != null;

        public bool IsSelectedPlanActive =>
            SelectedPlan != null &&
            !string.IsNullOrWhiteSpace(_progress.ActivePlanId) &&
            string.Equals(_progress.ActivePlanId, SelectedPlan.Id, StringComparison.OrdinalIgnoreCase);

        public bool IsSelectedPlanCompleted =>
            IsSelectedPlanActive &&
            SelectedPlan != null &&
            _progress.CompletedDayCount >= SelectedPlan.TotalDays;

        public bool CanStartSelectedPlan => SelectedPlan != null;

        public bool CanCompleteCurrentDay => IsSelectedPlanActive && _visibleDay != null;

        public string ActivePlanSummary
        {
            get
            {
                var activePlan = _readingPlanService.GetActivePlan();
                if (activePlan == null)
                {
                    return "No reading plan is active.";
                }

                if (_progress.CompletedDayCount >= activePlan.TotalDays)
                {
                    return $"{activePlan.Title} complete - {activePlan.TotalDays} of {activePlan.TotalDays} days finished.";
                }

                return $"{activePlan.Title} - Day {_progress.CompletedDayCount + 1} of {activePlan.TotalDays}.";
            }
        }

        public string SelectedPlanTitle => SelectedPlan?.Title ?? "Select a plan";

        public string SelectedPlanDescription => SelectedPlan?.Description ?? "Choose a built-in plan to see its details and current reading.";

        public string SelectedPlanMeta => SelectedPlan == null ? string.Empty : $"{SelectedPlan.TotalDays} days";

        public string ProgressSummary
        {
            get
            {
                if (SelectedPlan == null)
                {
                    return string.Empty;
                }

                if (IsSelectedPlanCompleted)
                {
                    return $"Completed - {SelectedPlan.TotalDays} of {SelectedPlan.TotalDays} days finished.";
                }

                if (IsSelectedPlanActive)
                {
                    return $"Current progress: Day {_progress.CompletedDayCount + 1} of {SelectedPlan.TotalDays}.";
                }

                if (_readingPlanService.GetActivePlan() != null)
                {
                    return "Starting this plan will replace the current active plan.";
                }

                return "This plan has not been started yet.";
            }
        }

        public string VisibleDayHeading
        {
            get
            {
                if (SelectedPlan == null)
                {
                    return string.Empty;
                }

                if (IsSelectedPlanCompleted)
                {
                    return "Plan complete";
                }

                if (_visibleDay == null)
                {
                    return "No reading available";
                }

                return IsSelectedPlanActive
                    ? $"Current reading - Day {_visibleDay.DayNumber}"
                    : $"Preview - Day {_visibleDay.DayNumber}";
            }
        }

        public string VisibleDayDescription
        {
            get
            {
                if (IsSelectedPlanCompleted)
                {
                    return "Restart the plan to begin again from day 1.";
                }

                if (_visibleDay == null)
                {
                    return "Select a plan to preview its first reading.";
                }

                return "Tap a passage below to open it in the Bible reader.";
            }
        }

        public string StartButtonText
        {
            get
            {
                if (SelectedPlan == null)
                {
                    return "Start plan";
                }

                return IsSelectedPlanActive ? "Restart plan" : "Start plan";
            }
        }

        public void Initialize()
        {
            if (Plans.Count == 0)
            {
                foreach (var plan in _readingPlanService.GetBuiltInPlans())
                {
                    Plans.Add(plan);
                }
            }

            Refresh();
        }

        public void Refresh()
        {
            _progress = _readingPlanService.GetProgress() ?? new ReadingPlanProgress();
            if (SelectedPlan == null)
            {
                SelectedPlan = _readingPlanService.GetActivePlan() ?? Plans.FirstOrDefault();
                return;
            }

            RefreshComputedState();
        }

        public void StartOrRestartSelectedPlan()
        {
            if (SelectedPlan == null)
            {
                return;
            }

            _readingPlanService.StartPlan(SelectedPlan.Id);
            Refresh();
        }

        public bool CompleteCurrentDay()
        {
            var completed = _readingPlanService.CompleteCurrentDay();
            Refresh();
            return completed;
        }

        private void RefreshComputedState()
        {
            _visibleDay = GetVisibleDay();
            ReplaceVisibleDayPassages(_visibleDay);

            OnPropertyChanged(nameof(HasSelectedPlan));
            OnPropertyChanged(nameof(HasVisibleDay));
            OnPropertyChanged(nameof(IsSelectedPlanActive));
            OnPropertyChanged(nameof(IsSelectedPlanCompleted));
            OnPropertyChanged(nameof(CanStartSelectedPlan));
            OnPropertyChanged(nameof(CanCompleteCurrentDay));
            OnPropertyChanged(nameof(ActivePlanSummary));
            OnPropertyChanged(nameof(SelectedPlanTitle));
            OnPropertyChanged(nameof(SelectedPlanDescription));
            OnPropertyChanged(nameof(SelectedPlanMeta));
            OnPropertyChanged(nameof(ProgressSummary));
            OnPropertyChanged(nameof(VisibleDayHeading));
            OnPropertyChanged(nameof(VisibleDayDescription));
            OnPropertyChanged(nameof(StartButtonText));
        }

        private ReadingPlanDay GetVisibleDay()
        {
            if (SelectedPlan == null)
            {
                return null;
            }

            if (IsSelectedPlanActive)
            {
                return _readingPlanService.GetCurrentDay();
            }

            return SelectedPlan.Days.FirstOrDefault();
        }

        private void ReplaceVisibleDayPassages(ReadingPlanDay day)
        {
            _visibleDayPassages.Clear();
            if (day?.Passages == null)
            {
                return;
            }

            foreach (var passage in day.Passages)
            {
                _visibleDayPassages.Add(passage);
            }
        }
    }
}
