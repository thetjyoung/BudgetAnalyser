using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BudgetAnalyser.Engine.Annotations;
using BudgetAnalyser.Engine.BankAccount;
using BudgetAnalyser.Engine.Budget;
using BudgetAnalyser.Engine.Ledger;
using BudgetAnalyser.Engine.Persistence;

namespace BudgetAnalyser.Engine.Services
{
    [AutoRegisterWithIoC(SingleInstance = true)]
    public class LedgerService : ILedgerService, ISupportsModelPersistence
    {
        private readonly IAccountTypeRepository accountTypeRepository;
        private readonly ILedgerBookRepository ledgerRepository;

        public LedgerService(
            [NotNull] ILedgerBookRepository ledgerRepository,
            [NotNull] IAccountTypeRepository accountTypeRepository)
        {
            if (ledgerRepository == null)
            {
                throw new ArgumentNullException(nameof(ledgerRepository));
            }

            if (accountTypeRepository == null)
            {
                throw new ArgumentNullException(nameof(accountTypeRepository));
            }

            this.ledgerRepository = ledgerRepository;
            this.accountTypeRepository = accountTypeRepository;
        }

        public event EventHandler Closed;
        public event EventHandler NewDataSourceAvailable;
        public event EventHandler Saved;
        public event EventHandler<AdditionalInformationRequestedEventArgs> Saving;
        public event EventHandler<ValidatingEventArgs> Validating;
        public ApplicationDataType DataType => ApplicationDataType.Ledger;

        public LedgerBook LedgerBook { get; private set; }
        public int LoadSequence => 50;

        public void Close()
        {
            LedgerBook = null;
            Closed?.Invoke(this, EventArgs.Empty);
        }

        public async Task CreateAsync(ApplicationDatabase applicationDatabase)
        {
            if (applicationDatabase.LedgerBookStorageKey.IsNothing())
            {
                throw new ArgumentNullException(nameof(applicationDatabase));
            }

            await this.ledgerRepository.CreateNewAndSaveAsync(applicationDatabase.LedgerBookStorageKey);
            await LoadAsync(applicationDatabase);
        }

        public async Task LoadAsync(ApplicationDatabase applicationDatabase)
        {
            if (applicationDatabase == null)
            {
                throw new ArgumentNullException(nameof(applicationDatabase));
            }

            LedgerBook = await this.ledgerRepository.LoadAsync(applicationDatabase.FullPath(applicationDatabase.LedgerBookStorageKey));

            NewDataSourceAvailable?.Invoke(this, EventArgs.Empty);
        }

        public void MoveLedgerToAccount(LedgerBucket ledger, Account storedInAccount)
        {
            if (ledger == null)
            {
                throw new ArgumentNullException(nameof(ledger));
            }
            if (storedInAccount == null)
            {
                throw new ArgumentNullException(nameof(storedInAccount));
            }

            LedgerBook.SetLedgerAccount(ledger, storedInAccount);
        }

        public void RemoveReconciliation(LedgerEntryLine line)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            LedgerBook.RemoveLine(line);
        }

        public void RenameLedgerBook(string newName)
        {
            if (newName == null)
            {
                throw new ArgumentNullException(nameof(newName));
            }

            LedgerBook.Name = newName;
        }

        public async Task SaveAsync(IReadOnlyDictionary<ApplicationDataType, object> contextObjects)
        {
            Saving?.Invoke(this, new AdditionalInformationRequestedEventArgs());

            var messages = new StringBuilder();
            if (!LedgerBook.Validate(messages))
            {
                throw new ValidationWarningException("Ledger Book is invalid, cannot save at this time:\n" + messages);
            }

            await this.ledgerRepository.SaveAsync(LedgerBook, LedgerBook.StorageKey);
            Saved?.Invoke(this, EventArgs.Empty);
        }

        public void SavePreview(IDictionary<ApplicationDataType, object> contextObjects)
        {
        }

        public LedgerBucket TrackNewBudgetBucket(ExpenseBucket bucket, Account storeInThisAccount)
        {
            if (bucket == null)
            {
                throw new ArgumentNullException(nameof(bucket));
            }
            if (storeInThisAccount == null)
            {
                throw new ArgumentNullException(nameof(storeInThisAccount));
            }

            return LedgerBook.AddLedger(bucket, storeInThisAccount);
        }

        public bool ValidateModel(StringBuilder messages)
        {
            EventHandler<ValidatingEventArgs> handler = Validating;
            handler?.Invoke(this, new ValidatingEventArgs());

            return LedgerBook.Validate(messages);
        }

        public IEnumerable<Account> ValidLedgerAccounts()
        {
            return this.accountTypeRepository.ListCurrentlyUsedAccountTypes();
        }
    }
}