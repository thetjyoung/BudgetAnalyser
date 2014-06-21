using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using BudgetAnalyser.Annotations;
using BudgetAnalyser.Budget;
using BudgetAnalyser.Dashboard;
using BudgetAnalyser.LedgerBook;
using BudgetAnalyser.Matching;
using BudgetAnalyser.ReportsCatalog;
using BudgetAnalyser.ShellDialog;
using BudgetAnalyser.Statement;
using Rees.Wpf;
using Rees.Wpf.ApplicationState;

namespace BudgetAnalyser
{
    public class ShellController : ControllerBase, IInitializableController
    {
        private readonly IPersistApplicationState statePersistence;
        private readonly UiContext uiContext;
        private bool initialised;
        private Point originalWindowSize;
        private Point originalWindowTopLeft;

        public ShellController(
            [NotNull] UiContext uiContext,
            [NotNull] IPersistApplicationState statePersistence)
        {
            if (uiContext == null)
            {
                throw new ArgumentNullException("uiContext");
            }

            if (statePersistence == null)
            {
                throw new ArgumentNullException("statePersistence");
            }

            MessengerInstance = uiContext.Messenger;
            MessengerInstance.Register<ShutdownMessage>(this, OnShutdownRequested);
            MessengerInstance.Register<ShellDialogRequestMessage>(this, OnDialogRequested);
            MessengerInstance.Register<ApplicationStateRequestedMessage>(this, OnApplicationStateRequested);
            MessengerInstance.Register<ApplicationStateLoadedMessage>(this, OnApplicationStateLoaded);
            this.statePersistence = statePersistence;
            this.uiContext = uiContext;
            BackgroundJob = uiContext.BackgroundJob;

            LedgerBookDialog = new ShellDialogController();
            DashboardDialog = new ShellDialogController();
            TransactionsDialog = new ShellDialogController();
            BudgetDialog = new ShellDialogController();
            ReportsDialog = new ShellDialogController();
        }

        public IBackgroundProcessingJobMetadata BackgroundJob { get; private set; }

        public BudgetController BudgetController
        {
            get { return this.uiContext.BudgetController; }
        }

        public ShellDialogController BudgetDialog { get; private set; }

        public DashboardController DashboardController
        {
            get { return this.uiContext.DashboardController; }
        }

        public ShellDialogController DashboardDialog { get; private set; }

        public LedgerBookController LedgerBookController
        {
            get { return this.uiContext.LedgerBookController; }
        }

        public ShellDialogController LedgerBookDialog { get; private set; }

        public MainMenuController MainMenuController
        {
            get { return this.uiContext.MainMenuController; }
        }

        public ReportsCatalogController ReportsCatalogController
        {
            get { return this.uiContext.ReportsCatalogController; }
        }

        public ShellDialogController ReportsDialog { get; private set; }

        public RulesController RulesController
        {
            get { return this.uiContext.RulesController; }
        }

        public StatementController StatementController
        {
            get { return this.uiContext.StatementController; }
        }

        public ShellDialogController TransactionsDialog { get; private set; }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Data binding")]
        public string WindowTitle
        {
            get { return "Budget Analyser"; }
        }

        internal Point WindowSize { get; private set; }
        internal Point WindowTopLeft { get; private set; }

        public void Initialize()
        {
            if (this.initialised)
            {
                return;
            }

            this.initialised = true;

            IList<IPersistent> rehydratedModels = this.statePersistence.Load().ToList();
            if (!rehydratedModels.OfType<LastBudgetLoadedV1>().Any())
            {
                // Mandatory budget file.
                rehydratedModels.Add(new LastBudgetLoadedV1());
            }

            // Create a distinct list of sequences.
            IEnumerable<int> sequences = rehydratedModels.Select(persistentModel => persistentModel.Sequence).OrderBy(s => s).Distinct();

            // Send state load messages in order.
            foreach (int sequence in sequences)
            {
                int sequenceCopy = sequence;
                IEnumerable<IPersistent> models = rehydratedModels.Where(persistentModel => persistentModel.Sequence == sequenceCopy);
                MessengerInstance.Send(new ApplicationStateLoadedMessage(models));
            }

            MessengerInstance.Send(new ApplicationStateLoadFinishedMessage());

            this.uiContext.Controllers.OfType<IInitializableController>().ToList().ForEach(i => i.Initialize());
        }

        public void NotifyOfWindowLocationChange(Point location)
        {
            WindowTopLeft = location;
        }

        public void NotifyOfWindowSizeChange(Point size)
        {
            WindowSize = size;
        }

        public void OnViewReady()
        {
            // Re-run the initialisers. This allows any controller who couldn't initialise until the views are loaded to now reattempt to initialise.
            this.uiContext.Controllers.OfType<IInitializableController>().ToList().ForEach(i => i.Initialize());
            if (this.originalWindowTopLeft != new Point())
            {
                WindowTopLeft = this.originalWindowTopLeft;
            }

            if (this.originalWindowSize != new Point())
            {
                WindowSize = this.originalWindowSize;
            }
        }

        private void OnApplicationStateLoaded(ApplicationStateLoadedMessage message)
        {
            if (!message.RehydratedModels.ContainsKey(typeof(ShellPersistentStateV1)))
            {
                return;
            }

            var shellState = message.RehydratedModels[typeof(ShellPersistentStateV1)].AdaptModel<ShellStateModel>();
            // Setting Window Size at this point has no effect, must happen after window is loaded. See OnViewReady()
            if (shellState.Size.X > 0 || shellState.Size.Y > 0)
            {
                this.originalWindowSize = shellState.Size;
            }
            else
            {
                this.originalWindowSize = new Point(1250, 600);
            }

            if (shellState.TopLeft.X > 0 || shellState.TopLeft.Y > 0)
            {
                // Setting Window Top & Left at this point has no effect, must happen after window is loaded. See OnViewReady()
                this.originalWindowTopLeft = shellState.TopLeft;
            }
        }

        private void OnApplicationStateRequested(ApplicationStateRequestedMessage message)
        {
            var shellPersistentStateV1 = new ShellPersistentStateV1
            {
                Model = new ShellStateModel
                {
                    Size = WindowSize,
                    TopLeft = WindowTopLeft,
                }
            };
            message.PersistThisModel(shellPersistentStateV1);
        }

        private void OnDialogRequested(ShellDialogRequestMessage message)
        {
            ShellDialogController appropriateController;
            switch (message.Location)
            {
                case BudgetAnalyserFeature.LedgerBook:
                    appropriateController = LedgerBookDialog;
                    break;

                case BudgetAnalyserFeature.Dashboard:
                    appropriateController = DashboardDialog;
                    break;

                case BudgetAnalyserFeature.Budget:
                    appropriateController = BudgetDialog;
                    break;

                case BudgetAnalyserFeature.Transactions:
                    appropriateController = TransactionsDialog;
                    break;

                case BudgetAnalyserFeature.Reports:
                    appropriateController = ReportsDialog;
                    break;

                default:
                    throw new NotSupportedException("The requested shell dialog location is not supported: " + message.Location);
            }

            appropriateController.Title = message.Title;
            appropriateController.Content = message.Content;
            appropriateController.DialogType = message.DialogType;
            appropriateController.CorrelationId = message.CorrelationId;
        }

        private void OnShutdownRequested(ShutdownMessage message)
        {
            var gatherDataMessage = new ApplicationStateRequestedMessage();
            MessengerInstance.Send(gatherDataMessage);
            this.statePersistence.Persist(gatherDataMessage.PersistentData);
        }
    }
}