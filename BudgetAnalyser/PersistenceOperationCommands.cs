﻿using System.Windows.Input;
using BudgetAnalyser.Engine;
using BudgetAnalyser.Annotations;
using GalaSoft.MvvmLight.CommandWpf;

namespace BudgetAnalyser
{
    [AutoRegisterWithIoC]
    public static class PersistenceOperationCommands
    {
        public static ICommand CreateNewDatabaseCommand { get; } = new RelayCommand(() => PersistenceOperations.OnCreateNewDatabaseCommandExecute());
        public static ICommand LoadDatabaseCommand { get; } = new RelayCommand(() => PersistenceOperations.OnLoadDatabaseCommandExecute());
        public static ICommand LoadDemoDatabaseCommand { get; } = new RelayCommand(() => PersistenceOperations.OnLoadDemoDatabaseCommandExecute());

        [PropertyInjection]
        public static PersistenceOperations PersistenceOperations { get; [UsedImplicitly] set; }

        // These properties use fields rather than new'ing up instance for each call because this is a shared static class consumed by many forms.
        public static ICommand SaveDatabaseCommand { get; } = new RelayCommand(() => PersistenceOperations.OnSaveDatabaseCommandExecute(), () => PersistenceOperations.HasUnsavedChanges);

        [UsedImplicitly]
        public static ICommand ValidateModelCommand { get; } = new RelayCommand(() => PersistenceOperations.OnValidateModelsCommandExecute());
    }
}