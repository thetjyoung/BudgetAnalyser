using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BudgetAnalyser.Annotations;
using BudgetAnalyser.Budget;
using BudgetAnalyser.Dashboard;
using BudgetAnalyser.Engine;
using BudgetAnalyser.Engine.Services;
using BudgetAnalyser.LedgerBook;
using BudgetAnalyser.Matching;
using BudgetAnalyser.ReportsCatalog;
using BudgetAnalyser.ShellDialog;
using BudgetAnalyser.Statement;
using Rees.UserInteraction.Contracts;
using Rees.Wpf;
using Rees.Wpf.ApplicationState;

namespace BudgetAnalyser
{
    public class ShellController : ControllerBase, IInitializableController
    {
        private readonly PersistenceOperations persistenceOperations;
        private readonly IPersistApplicationState statePersistence;
        private readonly IUiContext uiContext;
        private bool initialised;
        private Point originalWindowSize;
        private Point originalWindowTopLeft;

        public ShellController(
            [NotNull] IUiContext uiContext,
            [NotNull] IPersistApplicationState statePersistence,
            [NotNull] IDashboardService dashboardService,
            [NotNull] PersistenceOperations persistenceOperations
            )
        {
            if (uiContext == null)
            {
                throw new ArgumentNullException(nameof(uiContext));
            }

            if (statePersistence == null)
            {
                throw new ArgumentNullException(nameof(statePersistence));
            }

            if (dashboardService == null)
            {
                throw new ArgumentNullException(nameof(dashboardService));
            }

            if (persistenceOperations == null)
            {
                throw new ArgumentNullException(nameof(persistenceOperations));
            }

            MessengerInstance = uiContext.Messenger;
            MessengerInstance.Register<ShellDialogRequestMessage>(this, OnDialogRequested);
            MessengerInstance.Register<ApplicationStateRequestedMessage>(this, OnApplicationStateRequested);
            MessengerInstance.Register<ApplicationStateLoadedMessage>(this, OnApplicationStateLoaded);

            this.statePersistence = statePersistence;
            this.persistenceOperations = persistenceOperations;
            this.uiContext = uiContext;

            LedgerBookDialog = new ShellDialogController();
            DashboardDialog = new ShellDialogController();
            TransactionsDialog = new ShellDialogController();
            BudgetDialog = new ShellDialogController();
            ReportsDialog = new ShellDialogController();
        }

        [Engine.Annotations.UsedImplicitly]
        public BudgetController BudgetController => this.uiContext.BudgetController;

        public ShellDialogController BudgetDialog { get; }
        public DashboardController DashboardController => this.uiContext.DashboardController;
        public ShellDialogController DashboardDialog { get; }
        public bool HasUnsavedChanges => this.persistenceOperations.HasUnsavedChanges;

        [Engine.Annotations.UsedImplicitly]
        public LedgerBookController LedgerBookController => this.uiContext.LedgerBookController;

        public ShellDialogController LedgerBookDialog { get; }

        [Engine.Annotations.UsedImplicitly]
        public MainMenuController MainMenuController => this.uiContext.MainMenuController;

        [Engine.Annotations.UsedImplicitly]
        public ReportsCatalogController ReportsCatalogController => this.uiContext.ReportsCatalogController;

        public ShellDialogController ReportsDialog { get; }

        [Engine.Annotations.UsedImplicitly]
        public RulesController RulesController => this.uiContext.RulesController;

        [Engine.Annotations.UsedImplicitly]
        public StatementController StatementController => this.uiContext.StatementController;

        public ShellDialogController TransactionsDialog { get; }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Data binding")]
        [Engine.Annotations.UsedImplicitly]
        public string WindowTitle => "Budget Analyser";

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

            // Create a distinct list of sequences.
            IEnumerable<int> sequences = rehydratedModels.Select(persistentModel => persistentModel.LoadSequence).OrderBy(s => s).Distinct();

            this.uiContext.Controllers.OfType<IInitializableController>().ToList().ForEach(i => i.Initialize());

            // Send state load messages in order.
            foreach (int sequence in sequences)
            {
                int sequenceCopy = sequence;
                IEnumerable<IPersistent> models = rehydratedModels.Where(persistentModel => persistentModel.LoadSequence == sequenceCopy);
                MessengerInstance.Send(new ApplicationStateLoadedMessage(models));
            }

            MessengerInstance.Send(new ApplicationStateLoadFinishedMessage());
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

        /// <summary>
        ///     This method will persist the application state. Application State is user preference settings for the application,
        ///     window, and last loaded file.
        ///     Any data that is used for Budgets, reconciliation, reporting belongs in the Application Database.
        /// </summary>
        public void SaveApplicationState()
        {
            var gatherDataMessage = new ApplicationStateRequestedMessage();
            MessengerInstance.Send(gatherDataMessage);
            this.statePersistence.Persist(gatherDataMessage.PersistentData);
        }

        /// <summary>
        ///     Notify the ShellController the Shell is closing.
        /// </summary>
        public async Task<bool> ShellClosing()
        {
            if (this.persistenceOperations.HasUnsavedChanges)
            {
                bool? result = this.uiContext.UserPrompts.YesNoBox.Show("There are unsaved changes, save before exiting?", "Budget Analyser");
                if (result != null && result.Value)
                {
                    // Save must be run carefully because the application is exiting.  If run using the task factory with defaults the task will stall, as background tasks are waiting to be marshalled back to main context
                    // which is also waiting here, resulting in a deadlock.  This method will only work by first cancelling the close, awaiting this method and then re-triggering it.
                    await this.persistenceOperations.SaveDatabase();
                }

                return true;
            }

            return false;
        }

        private async void OnApplicationStateLoaded([NotNull] ApplicationStateLoadedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var shellState = message.ElementOfType<ShellPersistentStateV1>();
            if (shellState != null)
            {
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

            var storedMainAppState = message.ElementOfType<MainApplicationStateModelV1>();
            if (storedMainAppState != null)
            {
                try
                {
                    await this.persistenceOperations.LoadDatabase(storedMainAppState.BudgetAnalyserDataStorageKey);
                }
                catch (KeyNotFoundException)
                {
                    this.uiContext.UserPrompts.MessageBox.Show("Budget Analyser", "The previously loaded Budget Analyser file ({0}) no longer exists.", storedMainAppState.BudgetAnalyserDataStorageKey);
                }
            }
        }

        private void OnApplicationStateRequested(ApplicationStateRequestedMessage message)
        {
            var shellPersistentStateV1 = new ShellPersistentStateV1
            {
                Size = WindowSize,
                TopLeft = WindowTopLeft
            };
            message.PersistThisModel(shellPersistentStateV1);

            MainApplicationStateModelV1 dataFileState = this.persistenceOperations.PreparePersistentStateData();
            message.PersistThisModel(dataFileState);
        }

        private void OnDialogRequested(ShellDialogRequestMessage message)
        {
            ShellDialogController dialogController;
            switch (message.Location)
            {
                case BudgetAnalyserFeature.LedgerBook:
                    dialogController = LedgerBookDialog;
                    break;

                case BudgetAnalyserFeature.Dashboard:
                    dialogController = DashboardDialog;
                    break;

                case BudgetAnalyserFeature.Budget:
                    dialogController = BudgetDialog;
                    break;

                case BudgetAnalyserFeature.Transactions:
                    dialogController = TransactionsDialog;
                    break;

                case BudgetAnalyserFeature.Reports:
                    dialogController = ReportsDialog;
                    break;

                default:
                    throw new NotSupportedException("The requested shell dialog location is not supported: " + message.Location);
            }

            dialogController.Title = message.Title;
            dialogController.Content = message.Content;
            dialogController.DialogType = message.DialogType;
            dialogController.CorrelationId = message.CorrelationId;
            dialogController.HelpButtonVisible = message.HelpAvailable;
        }
    }
}