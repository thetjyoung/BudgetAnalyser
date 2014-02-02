﻿using System.Collections.Generic;
using System.Windows.Input;
using BudgetAnalyser.Engine;
using BudgetAnalyser.Engine.Account;
using BudgetAnalyser.Filtering;
using BudgetAnalyser.Statement;
using GalaSoft.MvvmLight.Command;
using Rees.Wpf;

namespace BudgetAnalyser.Dashboard
{
    [AutoRegisterWithIoC(SingleInstance = true)]
    public class DashboardController : ControllerBase, IShowableController
    {
        private IEnumerable<AccountType> currentAccountTypes;
        private bool doNotUseShown;

        public DashboardController(UiContext uiContext)
        {
            GlobalFilterController = uiContext.GlobalFilterController;

            MessagingGate.Register<StatementReadyMessage>(this, OnStatementReadyMessageReceived);
        }

        public ICommand GlobalAccountTypeFilterCommand
        {
            get { return new RelayCommand<FilterMode>(OnGlobalDateFilterCommandExecute, CanExecuteGlobalFilterCommand); }
        }

        public ICommand GlobalDateFilterCommand
        {
            get { return new RelayCommand<FilterMode>(OnGlobalDateFilterCommandExecute, CanExecuteGlobalFilterCommand); }
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

        public bool Shown
        {
            get { return this.doNotUseShown; }
            set
            {
                this.doNotUseShown = value;
                RaisePropertyChanged(() => Shown);
            }
        }

        private bool CanExecuteGlobalFilterCommand(FilterMode parameter)
        {
            return true;
        }

        private void OnGlobalDateFilterCommandExecute(FilterMode filterType)
        {
            if (filterType == FilterMode.Dates)
            {
                GlobalFilterController.PromptUserForDates();
            }
            else if (filterType == FilterMode.AccountType)
            {
                GlobalFilterController.PromptUserForAccountType(this.currentAccountTypes);
            }
        }

        private void OnStatementReadyMessageReceived(StatementReadyMessage message)
        {
            if (message.StatementModel != null)
            {
                this.currentAccountTypes = message.StatementModel.AccountTypes;
            }
        }
    }
}