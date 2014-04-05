﻿using BudgetAnalyser.Engine.Budget;

namespace BudgetAnalyser.Engine.Widget
{
    public class RemainingSurplusWidget : RemainingBudgetBucketWidget
    {
        public RemainingSurplusWidget()
        {
            DetailedText = "Surplus B";
            Name = "Surplus B";
            DependencyMissingToolTip = "A Statement, Budget, or a Filter are not present, surplus cannot be calculated.";
            RemainingBudgetToolTip = "Remaining Surplus for period is {0:C}";
            BucketCode = SurplusBucket.SurplusCode;
        }

        protected override decimal MonthlyBudgetAmount()
        {
            return Budget.Model.Surplus;
        }
    }
}