using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BudgetAnalyser.Annotations;
using BudgetAnalyser.Budget;
using BudgetAnalyser.Dashboard;
using BudgetAnalyser.Engine;
using BudgetAnalyser.Filtering;
using BudgetAnalyser.LedgerBook;
using BudgetAnalyser.OverallPerformance;
using BudgetAnalyser.ReportsCatalog;
using BudgetAnalyser.SpendingTrend;
using BudgetAnalyser.Statement;
using GalaSoft.MvvmLight.Messaging;
using Rees.UserInteraction.Contracts;
using Rees.Wpf;

namespace BudgetAnalyser
{
    /// <summary>
    ///     Controllers required by the <see cref="ShellController" /> grouped together for convenience.
    /// </summary>
    [AutoRegisterWithIoC(SingleInstance = true)]
    public class UiContext
    {
        private List<ControllerBase> controllers;

        public UiContext(
            [NotNull] IBudgetAnalysisView analysisFactory,
            [NotNull] IBackgroundProcessingJobMetadata backgroundJobMetadata,
            [NotNull] Func<IWaitCursor> waitCursorFactory,
            [NotNull] UserPrompts userPrompts,
            [NotNull] IMessenger messenger)
        {
            if (waitCursorFactory == null)
            {
                throw new ArgumentNullException("waitCursorFactory");
            }

            if (userPrompts == null)
            {
                throw new ArgumentNullException("userPrompts");
            }

            if (messenger == null)
            {
                throw new ArgumentNullException("messenger");
            }

            if (analysisFactory == null)
            {
                throw new ArgumentNullException("analysisFactory");
            }

            if (backgroundJobMetadata == null)
            {
                throw new ArgumentNullException("backgroundJobMetadata");
            }

            WaitCursorFactory = waitCursorFactory;
            AnalysisFactory = analysisFactory;
            BackgroundJob = backgroundJobMetadata;
            UserPrompts = userPrompts;
            Messenger = messenger;
        }

        public AddLedgerReconciliationController AddLedgerReconciliationController { get; set; }

        public IBudgetAnalysisView AnalysisFactory { get; private set; }
        public AppliedRulesController AppliedRulesController { get; set; }
        public IBackgroundProcessingJobMetadata BackgroundJob { get; private set; }

        public BudgetController BudgetController { get; set; }
        public BudgetPieController BudgetPieController { get; set; }
        public ChooseBudgetBucketController ChooseBudgetBucketController { get; set; }

        public IEnumerable<ControllerBase> Controllers
        {
            get { return this.controllers ?? (this.controllers = DiscoverAllControllers()); }
        }

        public DashboardController DashboardController { get; set; }

        public GlobalFilterController GlobalFilterController { get; set; }
        public LedgerBookController LedgerBookController { get; set; }
        public LedgerRemarksController LedgerRemarksController { get; set; }
        public LedgerTransactionsController LedgerTransactionsController { get; set; }
        public MainMenuController MainMenuController { get; set; }
        public IMessenger Messenger { get; private set; }
        public NewRuleController NewRuleController { get; set; }
        public ReportsCatalogController ReportsCatalogController { get; set; }
        public RulesController RulesController { get; set; }

        public IEnumerable<IShowableController> ShowableControllers
        {
            get { return Controllers.OfType<IShowableController>(); }
        }

        public SpendingTrendController SpendingTrendController { get; set; }
        public StatementController StatementController { get; set; }
        public UserPrompts UserPrompts { get; private set; }
        public Func<IWaitCursor> WaitCursorFactory { get; private set; }

        private List<ControllerBase> DiscoverAllControllers()
        {
            return GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => typeof(ControllerBase).IsAssignableFrom(p.PropertyType))
                .Select(p => p.GetValue(this))
                .Cast<ControllerBase>()
                .ToList();
        }
    }
}