﻿using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using BudgetAnalyser.Budget;
using BudgetAnalyser.Engine;
using BudgetAnalyser.Engine.Annotations;
using BudgetAnalyser.Engine.Budget;
using BudgetAnalyser.Engine.Services;
using BudgetAnalyser.Engine.Statement;
using BudgetAnalyser.Engine.Widgets;
using BudgetAnalyser.Filtering;
using BudgetAnalyser.LedgerBook;
using BudgetAnalyser.Statement;
using GalaSoft.MvvmLight.CommandWpf;
using Rees.UserInteraction.Contracts;
using Rees.Wpf;
using Rees.Wpf.ApplicationState;

namespace BudgetAnalyser.Dashboard
{
    //[SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Necessary in this case, this class is used to monitor all parts of the system.")]
    [AutoRegisterWithIoC(SingleInstance = true)]
    public sealed class DashboardController : ControllerBase, IShowableController
    {
        private Guid doNotUseCorrelationId;
        private bool doNotUseShown;
        private readonly ChooseBudgetBucketController chooseBudgetBucketController;
        private readonly CreateNewFixedBudgetController createNewFixedBudgetController;
        private readonly CreateNewSurprisePaymentMonitorController createNewSurprisePaymentMonitorController;
        private readonly IDashboardService dashboardService;
        private readonly IUserMessageBox messageBox;
        // TODO Support for image changes when widget updates

        public DashboardController(
            [NotNull] UiContext uiContext,
            [NotNull] ChooseBudgetBucketController chooseBudgetBucketController,
            [NotNull] CreateNewFixedBudgetController createNewFixedBudgetController,
            [NotNull] CreateNewSurprisePaymentMonitorController createNewSurprisePaymentMonitorController,
            [NotNull] IDashboardService dashboardService)
        {
            if (uiContext == null)
            {
                throw new ArgumentNullException("uiContext");
            }

            if (chooseBudgetBucketController == null)
            {
                throw new ArgumentNullException("chooseBudgetBucketController");
            }

            if (createNewFixedBudgetController == null)
            {
                throw new ArgumentNullException("createNewFixedBudgetController");
            }

            if (dashboardService == null)
            {
                throw new ArgumentNullException("dashboardService");
            }

            this.chooseBudgetBucketController = chooseBudgetBucketController;
            this.createNewFixedBudgetController = createNewFixedBudgetController;
            this.createNewSurprisePaymentMonitorController = createNewSurprisePaymentMonitorController;
            this.messageBox = uiContext.UserPrompts.MessageBox;
            this.dashboardService = dashboardService;

            this.chooseBudgetBucketController.Chosen += OnBudgetBucketChosenForNewBucketMonitor;
            this.createNewFixedBudgetController.Complete += OnCreateNewFixedProjectComplete;
            this.createNewSurprisePaymentMonitorController.Complete += OnCreateNewSurprisePaymentMonitorComplete;

            GlobalFilterController = uiContext.GlobalFilterController;
            CorrelationId = Guid.NewGuid();

            RegisterForMessengerNotifications(uiContext);
        }

        public Guid CorrelationId
        {
            get { return this.doNotUseCorrelationId; }
            private set
            {
                this.doNotUseCorrelationId = value;
                RaisePropertyChanged(() => CorrelationId);
            }
        }

        public GlobalFilterController GlobalFilterController { get; private set; }

        public string VersionString
        {
            get
            {
                var assemblyName = GetType().Assembly.GetName();
                return assemblyName.Name + "Version: " + assemblyName.Version;
            }
        }

        public ICommand WidgetCommand
        {
            get { return new RelayCommand<Widget>(OnWidgetCommandExecuted, WidgetCommandCanExecute); }
        }

        public ObservableCollection<WidgetGroup> WidgetGroups { get; private set; }

        public bool Shown
        {
            get { return this.doNotUseShown; }
            set
            {
                if (value == this.doNotUseShown)
                {
                    return;
                }
                this.doNotUseShown = value;
                RaisePropertyChanged(() => Shown);
            }
        }

        private void OnApplicationStateLoadedMessageReceived([NotNull] ApplicationStateLoadedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (!message.RehydratedModels.ContainsKey(typeof(DashboardApplicationStateV1)))
            {
                return;
            }

            var storedState = message.RehydratedModels[typeof(DashboardApplicationStateV1)].AdaptModel<MainApplicationStateModel>();
            if (storedState == null)
            {
                return;
            }

            // Now that we have the previously persisted state data we can properly intialise the service.
            WidgetGroups = this.dashboardService.LoadPersistedStateData(storedState);
        }

        private void OnApplicationStateRequested(ApplicationStateRequestedMessage message)
        {
            var widgetStates = this.dashboardService.PreparePersistentStateData();

            message.PersistThisModel(new DashboardApplicationStateV1 { Model = widgetStates });
        }

        private void OnBudgetBucketChosenForNewBucketMonitor(object sender, BudgetBucketChosenEventArgs args)
        {
            if (args.CorrelationId != CorrelationId)
            {
                return;
            }

            CorrelationId = Guid.NewGuid();
            var bucket = this.chooseBudgetBucketController.Selected;
            if (bucket == null)
            {
                // Cancelled by user.
                return;
            }

            var widget = this.dashboardService.CreateNewBucketMonitorWidget(bucket.Code);
            if (widget == null)
            {
                this.messageBox.Show("New Budget Bucket Widget", "This Budget Bucket Monitor Widget for [{0}] already exists.", bucket.Code);
            }
        }

        private void OnBudgetReadyMessageReceived([NotNull] BudgetReadyMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (message.Budgets == null)
            {
                throw new InvalidOperationException("The budgets collection should never be null.");
            }

            this.dashboardService.NotifyOfDependencyChange(message.Budgets);
            this.dashboardService.NotifyOfDependencyChange<IBudgetCurrencyContext>(message.ActiveBudget);
        }

        private void OnCreateNewSurprisePaymentMonitorComplete(object sender, DialogResponseEventArgs dialogResponseEventArgs)
        {
            if (dialogResponseEventArgs.Canceled || dialogResponseEventArgs.CorrelationId != CorrelationId)
            {
                return;
            }

            CorrelationId = Guid.NewGuid();
            try
            {
                this.dashboardService.CreateNewSurprisePaymentMonitorWidget(
                    this.createNewSurprisePaymentMonitorController.Selected.Code,
                    this.createNewSurprisePaymentMonitorController.PaymentStartDate,
                    this.createNewSurprisePaymentMonitorController.Frequency);
            }
            catch (ArgumentException ex)
            {
                this.messageBox.Show(ex.Message, "Unable to create new surprise payment monitor widget.");
            }
        }

        private void OnCreateNewFixedProjectComplete(object sender, DialogResponseEventArgs dialogResponseEventArgs)
        {
            if (dialogResponseEventArgs.Canceled || dialogResponseEventArgs.CorrelationId != CorrelationId)
            {
                return;
            }

            CorrelationId = Guid.NewGuid();
            try
            {
                this.dashboardService.CreateNewFixedBudgetMonitorWidget(
                    this.createNewFixedBudgetController.Code,
                    this.createNewFixedBudgetController.Description,
                    this.createNewFixedBudgetController.Amount);
            }
            catch (ArgumentException ex)
            {
                this.messageBox.Show(ex.Message, "Unable to create new fixed budget project");
            }
        }

        private void OnFilterAppliedMessageReceived([NotNull] FilterAppliedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (message.Criteria == null)
            {
                throw new InvalidOperationException("The Criteria object should never be null.");
            }

            this.dashboardService.NotifyOfDependencyChange(message.Criteria);
        }

        private void OnLedgerBookReadyMessageReceived([NotNull] LedgerBookReadyMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (message.LedgerBook == null)
            {
                this.dashboardService.NotifyOfDependencyChange<Engine.Ledger.LedgerBook>(null);
            }
            else
            {
                this.dashboardService.NotifyOfDependencyChange(message.LedgerBook);
            }
        }

        private void OnStatementModifiedMessagedReceived([NotNull] StatementHasBeenModifiedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            // TODO Can this logic be moved to the Dashboard Service.
            if (message.StatementModel == null)
            {
                return;
            }

            this.dashboardService.NotifyOfDependencyChange(message.StatementModel);
        }

        private void OnStatementReadyMessageReceived([NotNull] StatementReadyMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (message.StatementModel == null)
            {
                this.dashboardService.NotifyOfDependencyChange<StatementModel>(null);
            }
            else
            {
                this.dashboardService.NotifyOfDependencyChange(message.StatementModel);
            }
        }

        private void OnWidgetCommandExecuted(Widget widget)
        {
            MessengerInstance.Send(new WidgetActivatedMessage(widget));
        }

        private void RegisterForMessengerNotifications(UiContext uiContext)
        {
            // Register for all dependent objects change messages.
            MessengerInstance = uiContext.Messenger;
            MessengerInstance.Register<StatementReadyMessage>(this, OnStatementReadyMessageReceived);
            MessengerInstance.Register<StatementHasBeenModifiedMessage>(this, OnStatementModifiedMessagedReceived);
            MessengerInstance.Register<ApplicationStateLoadedMessage>(this, OnApplicationStateLoadedMessageReceived);
            MessengerInstance.Register<ApplicationStateRequestedMessage>(this, OnApplicationStateRequested);
            MessengerInstance.Register<BudgetReadyMessage>(this, OnBudgetReadyMessageReceived);
            MessengerInstance.Register<FilterAppliedMessage>(this, OnFilterAppliedMessageReceived);
            MessengerInstance.Register<LedgerBookReadyMessage>(this, OnLedgerBookReadyMessageReceived);
        }

        private static bool WidgetCommandCanExecute(Widget widget)
        {
            return widget.Clickable;
        }
    }
}