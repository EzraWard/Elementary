using Elementary.Core.Models;
using System.Collections.Generic;

namespace Elementary.Core.Interfaces
{
    public interface IReadingPlanService
    {
        IReadOnlyList<ReadingPlan> GetBuiltInPlans();
        ReadingPlanProgress GetProgress();
        ReadingPlan GetActivePlan();
        ReadingPlanDay GetCurrentDay();
        void StartPlan(string planId);
        void ClearActivePlan();
        bool CompleteCurrentDay();
    }
}
