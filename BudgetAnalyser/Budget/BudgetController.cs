using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using BudgetAnalyser.Annotations;
using BudgetAnalyser.Engine;
using BudgetAnalyser.Engine.Budget;
using GalaSoft.MvvmLight.Command;
using Rees.UserInteraction.Contracts;
using Rees.Wpf;
using Rees.Wpf.ApplicationState;
using FileFormatException = BudgetAnalyser.Engine.FileFormatException;

namespace BudgetAnalyser.Budget
{
    public class BudgetController : ControllerBase, IShowableController
    {
        private const string CloseBudgetMenuName = "Close _Budget";
        private const string EditBudgetMenuName = "Edit Current _Budget";

        private readonly IViewLoader budgetDetailsViewLoader;
        private readonly IBudgetModelImporter budgetModelImporter;
        private readonly IViewLoader budgetSelectionLoader;
        private readonly Func<IUserPromptOpenFile> fileOpenDialogFactory;
        private readonly Func<IUserPromptSaveFile> fileSaveDialogFactory;
        private readonly IUserInputBox inputBox;
        private readonly IUserMessageBox messageBox;
        private readonly IUserQuestionBoxYesNo questionBox;
        private string budgetMenuItemName;
        private bool dirty;
        private BudgetCurrencyContext doNotUseModel;
        private bool doNotUseShownBudget;
        private decimal expenseTotal;
        private decimal incomeTotal;
        private decimal surplus;

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "OnPropertyChange is ok to call here")]
        public BudgetController(
            [NotNull] IBudgetModelImporter budgetModelImporter,
            [NotNull] UiContext context,
            [NotNull] IViewLoader budgetDetailsViewLoader,
            [NotNull] IViewLoader budgetSelectionLoader)
        {
            // BUG Scroll into view when adding new expense or income.
            if (budgetModelImporter == null)
            {
                throw new ArgumentNullException("budgetModelImporter");
            }

            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (budgetDetailsViewLoader == null)
            {
                throw new ArgumentNullException("budgetDetailsViewLoader");
            }

            if (budgetSelectionLoader == null)
            {
                throw new ArgumentNullException("budgetSelectionLoader");
            }

            this.budgetSelectionLoader = budgetSelectionLoader;
            this.budgetModelImporter = budgetModelImporter;
            this.questionBox = context.UserPrompts.YesNoBox;
            this.messageBox = context.UserPrompts.MessageBox;
            this.fileOpenDialogFactory = context.UserPrompts.OpenFileFactory;
            this.fileSaveDialogFactory = context.UserPrompts.SaveFileFactory;
            this.inputBox = context.UserPrompts.InputBox;
            this.budgetDetailsViewLoader = budgetDetailsViewLoader;
            Shown = false;

            MessagingGate.Register<ApplicationStateRequestedMessage>(this, OnApplicationStateRequested);
            MessagingGate.Register<ApplicationStateLoadedMessage>(this, OnApplicationStateLoaded);


            var budget = new BudgetModel();
            CurrentBudget = new BudgetCurrencyContext(new BudgetCollection(new[] { budget }), budget);
        }

        public ICommand AddNewExpenseCommand
        {
            get { return new RelayCommand<ExpenseBudgetBucket>(OnAddNewExpenseExecute); }
        }

        public ICommand AddNewIncomeCommand
        {
            get { return new RelayCommand(OnAddNewIncomeExecute); }
        }

        public string BudgetMenuItemName
        {
            get { return this.budgetMenuItemName; }

            set
            {
                this.budgetMenuItemName = value;
                RaisePropertyChanged(() => BudgetMenuItemName);
            }
        }

        public BudgetCollection Budgets { get; private set; }

        public ICommand CloseCommand
        {
            get { return new RelayCommand(OnCloseCommandExecute); }
        }

        public BudgetCurrencyContext CurrentBudget
        {
            get { return this.doNotUseModel; }

            private set
            {
                this.doNotUseModel = value;
                ReleaseListBindingEvents(Incomes);
                ReleaseListBindingEvents(Expenses);
                if (this.doNotUseModel == null)
                {
                    Incomes = null;
                    Expenses = null;
                }
                else
                {
                    Incomes = new BindingList<Income>(this.doNotUseModel.Model.Incomes.ToList());
                    Incomes.ToList().ForEach(i => i.PropertyChanged += OnIncomeAmountPropertyChanged);
                    Expenses = new BindingList<Expense>(this.doNotUseModel.Model.Expenses);
                    Expenses.ToList().ForEach(e => e.PropertyChanged += OnExpenseAmountPropertyChanged);
                }

                RaisePropertyChanged(() => Incomes);
                RaisePropertyChanged(() => Expenses);
                OnExpenseAmountPropertyChanged(null, EventArgs.Empty);
                OnIncomeAmountPropertyChanged(null, EventArgs.Empty);
                RaisePropertyChanged(() => CurrentBudget);
            }
        }

        public ICommand DeleteBudgetItemCommand
        {
            get { return new RelayCommand<object>(OnDeleteBudgetItemCommandExecute); }
        }

        public ICommand DetailsCommand
        {
            get { return new RelayCommand(OnDetailsCommandExecute); }
        }

        public decimal ExpenseTotal
        {
            get { return this.expenseTotal; }

            private set
            {
                this.expenseTotal = value;
                RaisePropertyChanged(() => ExpenseTotal);
            }
        }

        public BindingList<Expense> Expenses { get; private set; }

        public decimal IncomeTotal
        {
            get { return this.incomeTotal; }

            private set
            {
                this.incomeTotal = value;
                RaisePropertyChanged(() => IncomeTotal);
            }
        }

        public BindingList<Income> Incomes { get; private set; }

        public ICommand LoadBudgetCommand
        {
            get { return new RelayCommand(OnLoadBudgetCommandExecute); }
        }

        public ICommand SaveAsCommand
        {
            get { return new RelayCommand(OnSaveAsCommandExecute); }
        }

        public bool Shown
        {
            get { return this.doNotUseShownBudget; }

            set
            {
                this.doNotUseShownBudget = value;
                RaisePropertyChanged(() => Shown);
                BudgetMenuItemName = this.doNotUseShownBudget ? CloseBudgetMenuName : EditBudgetMenuName;
            }
        }

        public decimal Surplus
        {
            get { return this.surplus; }
            private set
            {
                this.surplus = value;
                RaisePropertyChanged(() => Surplus);
            }
        }

        public string TruncatedFileName
        {
            get { return Budgets.FileName.TruncateLeft(100, true); }
        }

        public void SelectOtherBudget()
        {
            this.budgetSelectionLoader.ShowDialog(this);
        }

        public void ShowOtherBudget(BudgetModel budgetToShow)
        {
            CurrentBudget = new BudgetCurrencyContext(Budgets, budgetToShow);
            Shown = true;
            this.dirty = false; // Need to reset this because events fire needlessly (in this case) as a result of setting the CurrentBudget.
        }

        public bool ValidateAndClose()
        {
            bool valid = ValidateAndSaveIfRequired();
            if (valid)
            {
                if (CurrentBudget.Model != Budgets.CurrentActiveBudget)
                {
                    // Were viewing a different budget other than the current active budget for today's date.  Reset back to active budget.
                    CurrentBudget = new BudgetCurrencyContext(Budgets, Budgets.CurrentActiveBudget);
                    this.dirty = false;
                }

                Messenger.Send(new BudgetReadyMessage(CurrentBudget));
            }

            return valid;
        }

        protected virtual string GetDefaultFileName()
        {
            string path = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            return Path.Combine(path, "BudgetModel.xml");
        }

        protected virtual bool SaveToDiskAsXml()
        {
            string input = this.inputBox.Show("Budget Maintenance", "Enter an optional comment to describe what you changed.");
            if (input == null)
            {
                return false;
            }

            CurrentBudget.Model.LastModifiedComment = input;
            this.budgetModelImporter.SaveBudgetData(Budgets);
            return true;
        }

        private string GetFileNameFromUserForOpen()
        {
            IUserPromptOpenFile fileOpenDialog = this.fileOpenDialogFactory();
            fileOpenDialog.CheckFileExists = true;
            fileOpenDialog.CheckPathExists = true;
            bool? result = fileOpenDialog.ShowDialog();
            if (result == null || result == false)
            {
                return null;
            }

            return fileOpenDialog.FileName;
        }

        private string GetFileNameFromUserForSave()
        {
            IUserPromptSaveFile fileSaveDialog = this.fileSaveDialogFactory();
            fileSaveDialog.CheckPathExists = true;
            fileSaveDialog.AddExtension = true;
            fileSaveDialog.DefaultExt = ".xml";
            bool? result = fileSaveDialog.ShowDialog();
            if (result == null || result == false)
            {
                return null;
            }

            return fileSaveDialog.FileName;
        }

        private void HandleBudgetFileExceptions(string message)
        {
            string defaultFileName = GetDefaultFileName();
            this.messageBox.Show("Budget File", "{0}\n{1}", message, defaultFileName);
            LoadBudget(defaultFileName);
        }

        private void LoadBudget(string fileName)
        {
            Budgets = this.budgetModelImporter.LoadBudgetData(fileName);
            BudgetBucketBindingSource.BucketRepository = this.budgetModelImporter.BudgetBucketRepository;
            CurrentBudget = new BudgetCurrencyContext(Budgets, Budgets.CurrentActiveBudget);
            RaisePropertyChanged(() => TruncatedFileName);
        }

        private void OnAddNewExpenseExecute(ExpenseBudgetBucket expense)
        {
            this.dirty = true;
            Expense newExpense;
            if (expense is SpentMonthlyExpense)
            {
                newExpense = new Expense { Bucket = new SpentMonthlyExpense(string.Empty, string.Empty), Amount = 0 };
            }
            else if (expense is SavedUpForExpense)
            {
                newExpense = new Expense { Bucket = new SavedUpForExpense(string.Empty, string.Empty), Amount = 0 };
            }
            else
            {
                throw new InvalidCastException("Invalid type passed to Add New Expense: " + expense);
            }

            Expenses.Add(newExpense);
            newExpense.PropertyChanged += OnExpenseAmountPropertyChanged;
        }

        private void OnAddNewIncomeExecute()
        {
            this.dirty = true;
            var newIncome = new Income { Bucket = new IncomeBudgetBucket(string.Empty, string.Empty), Amount = 0 };
            Incomes.Add(newIncome);
            newIncome.PropertyChanged += OnIncomeAmountPropertyChanged;
        }

        private void OnApplicationStateLoaded(ApplicationStateLoadedMessage message)
        {
            try
            {
                string defaultFileName = GetDefaultFileName();
                if (!message.RehydratedModels.ContainsKey(typeof (LastBudgetLoadedV1)))
                {
                    LoadBudget(defaultFileName);
                    return;
                }

                var budgetFileName = message.RehydratedModels[typeof (LastBudgetLoadedV1)].AdaptModel<string>();
                if (string.IsNullOrWhiteSpace(budgetFileName))
                {
                    LoadBudget(defaultFileName);
                    return;
                }

                LoadBudget(budgetFileName);
            }
            catch (FileFormatException)
            {
                HandleBudgetFileExceptions("The last Budget file is an invalid file format. A empty default file will use the default file instead.");
            }
            catch (FileNotFoundException)
            {
                HandleBudgetFileExceptions("The last Budget file used cannot be found. A empty default file will use the default file instead.");
            }
            finally
            {
                if (CurrentBudget != null)
                {
                    Messenger.Send(new BudgetReadyMessage(CurrentBudget));
                }
            }
        }

        private void OnApplicationStateRequested(ApplicationStateRequestedMessage message)
        {
            // Only the filename of the current budget is saved using the ApplicationState mechanism.  The budget itself is saved on demand when it has changed.
            // Save the filename of the last budget used by the application.
            var persistentModel = new LastBudgetLoadedV1 { Model = Budgets.FileName };
            message.PersistThisModel(persistentModel);
        }

        private void OnCloseCommandExecute()
        {
            ValidateAndClose();
        }

        private void OnDeleteBudgetItemCommandExecute(object budgetItem)
        {
            this.dirty = true;
            var expenseItem = budgetItem as Expense;
            if (expenseItem != null)
            {
                expenseItem.PropertyChanged -= OnExpenseAmountPropertyChanged;
                Expenses.Remove(expenseItem);
                return;
            }

            var incomeItem = budgetItem as Income;
            if (incomeItem != null)
            {
                incomeItem.PropertyChanged -= OnIncomeAmountPropertyChanged;
                Incomes.Remove(incomeItem);
            }
        }

        private void OnDetailsCommandExecute()
        {
            this.budgetDetailsViewLoader.ShowDialog(CurrentBudget);
        }

        private void OnExpenseAmountPropertyChanged(object sender, EventArgs propertyChangedEventArgs)
        {
            // Let the first property change event through, because it is the initial set of the value.
            if (ExpenseTotal != 0)
            {
                this.dirty = true;
            }

            ExpenseTotal = Expenses.Sum(x => x.Amount);
            Surplus = IncomeTotal - ExpenseTotal;
        }

        private void OnIncomeAmountPropertyChanged(object sender, EventArgs propertyChangedEventArgs)
        {
            // Let the first property change event through, because it is the initial set of the value.
            if (IncomeTotal != 0)
            {
                this.dirty = true;
            }

            IncomeTotal = Incomes.Sum(x => x.Amount);
            Surplus = IncomeTotal - ExpenseTotal;
        }

        private void OnLoadBudgetCommandExecute()
        {
            bool valid = ValidateAndSaveIfRequired();
            if (!valid)
            {
                return;
            }

            this.dirty = false;
            string fileName = GetFileNameFromUserForOpen();
            LoadBudget(fileName);
        }

        private void OnSaveAsCommandExecute()
        {
            string fileName = GetFileNameFromUserForSave();
            if (fileName == null)
            {
                return;
            }

            this.dirty = true;
            Budgets.FileName = fileName;
            SaveBudgetModel();
            RaisePropertyChanged(() => TruncatedFileName);
        }

        private void ReleaseListBindingEvents(IEnumerable<BudgetItem> items)
        {
            if (items == null)
            {
                return;
            }

            foreach (BudgetItem item in items)
            {
                item.PropertyChanged -= OnIncomeAmountPropertyChanged;
                item.PropertyChanged -= OnExpenseAmountPropertyChanged;
            }
        }

        private bool SaveBudgetModel()
        {
            // Copy view model bound data back into model.
            CurrentBudget.Model.Update(Incomes, Expenses);
            var validationMessages = new StringBuilder();
            if (!Budgets.Validate(validationMessages))
            {
                this.messageBox.Show(validationMessages.ToString(), "Unable to save, some data is invalid");
                return false;
            }

            if (SaveToDiskAsXml())
            {
                this.dirty = false;
                return true;
            }

            return false;
        }

        private bool ValidateAndSaveIfRequired()
        {
            bool valid = true;

            // If no changes made to the budget model data return straight away.
            if (this.dirty)
            {
                bool? decision = this.questionBox.Show("Save changes to the budget?", "Edit Budget");
                if (decision != null && decision == true)
                {
                    // Yes, please save the changes.
                    valid = SaveBudgetModel();
                }
                else
                {
                    // No thanks, discard the changes. To do this, we'll need to revert from file.
                    LoadBudget(Budgets.FileName);
                }
            }

            return valid;
        }
    }
}