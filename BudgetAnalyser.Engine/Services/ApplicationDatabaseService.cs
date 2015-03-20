using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BudgetAnalyser.Engine.Annotations;
using BudgetAnalyser.Engine.Persistence;

namespace BudgetAnalyser.Engine.Services
{
    [AutoRegisterWithIoC(SingleInstance = true)]
    public class ApplicationDatabaseService : IApplicationDatabaseService
    {
        private readonly IApplicationDatabaseRepository applicationRepository;
        private readonly IEnumerable<IApplicationDatabaseDependent> databaseDependents;
        private readonly Dictionary<ApplicationDataType, bool> dirtyData = new Dictionary<ApplicationDataType, bool>();
        private ApplicationDatabase budgetAnalyserDatabase;

        public ApplicationDatabaseService(
            [NotNull] IApplicationDatabaseRepository applicationRepository,
            [NotNull] IEnumerable<IApplicationDatabaseDependent> databaseDependents)

        {
            if (applicationRepository == null)
            {
                throw new ArgumentNullException("applicationRepository");
            }

            if (databaseDependents == null)
            {
                throw new ArgumentNullException("databaseDependents");
            }

            this.applicationRepository = applicationRepository;
            this.databaseDependents = databaseDependents.OrderBy(d => d.LoadSequence).ToList();
            InitialiseDirtyDataTable();
        }

        /// <summary>
        ///     Gets or sets a value indicating whether there are unsaved changes across all application data.
        /// </summary>
        public bool HasUnsavedChanges
        {
            get { return this.dirtyData.Values.Any(v => v); }
        }

        /// <summary>
        ///     Closes the currently loaded Budget Analyser file, and therefore any other application data is also closed.
        ///     Changes are discarded, no prompt or error will occur if there are unsaved changes. This check should be done before
        ///     calling this method.
        /// </summary>
        public ApplicationDatabase Close()
        {
            foreach (IApplicationDatabaseDependent service in this.databaseDependents.OrderByDescending(d => d.LoadSequence))
            {
                service.Close();
            }

            ClearDirtyDataFlags();

            this.budgetAnalyserDatabase.Close();
            return this.budgetAnalyserDatabase;
        }

        /// <summary>
        ///     Loads the specified Budget Analyser file by file name.
        ///     No warning will be given if there is any unsaved data. This should be checked before calling this method.
        /// </summary>
        /// <param name="storageKey">Name and path to the file.</param>
        public async Task<ApplicationDatabase> Load(string storageKey)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
            {
                throw new ArgumentNullException("storageKey");
            }

            ClearDirtyDataFlags();

            this.budgetAnalyserDatabase = await this.applicationRepository.LoadAsync(storageKey);
            try
            {
                foreach (IApplicationDatabaseDependent service in this.databaseDependents) // Already sorted ascending by sequence number.
                {
                    await service.LoadAsync(this.budgetAnalyserDatabase);
                }
            }
            catch (DataFormatException ex)
            {
                Close();
                throw new DataFormatException("A subordindate data file is invalid or corrupt unable to load " + storageKey, ex);
            }
            catch (KeyNotFoundException ex)
            {
                Close();
                throw new KeyNotFoundException("A subordinate data file cannot be found: " + ex.Message, ex);
            }
            catch (NotSupportedException ex)
            {
                Close();
                throw new DataFormatException("A subordinate data file contains unsupported data.", ex);
            }

            return this.budgetAnalyserDatabase;
        }

        /// <summary>
        ///     Notifies the service that data has changed and will need to be saved.
        /// </summary>
        public void NotifyOfChange(ApplicationDataType dataType)
        {
            this.dirtyData[dataType] = true;
        }

        public MainApplicationStateModelV1 PreparePersistentStateData()
        {
            if (this.budgetAnalyserDatabase == null)
            {
                return new MainApplicationStateModelV1();
            }

            return new MainApplicationStateModelV1
            {
                BudgetAnalyserDataStorageKey = this.budgetAnalyserDatabase.FileName
            };
        }

        /// <summary>
        ///     Saves all Budget Analyser application data.
        /// </summary>
        public void Save()
        {
            if (this.budgetAnalyserDatabase == null)
            {
                throw new InvalidOperationException("Application Database cannot be null here. Code Bug.");
            }

            if (!HasUnsavedChanges)
            {
                return;
            }

            // TODO Validate before save
            // TODO Save data only when valid

            this.budgetAnalyserDatabase.LedgerReconciliationToDoCollection.Clear(); // Only clears system generated tasks, not persistent user created tasks.
            this.applicationRepository.Save(this.budgetAnalyserDatabase);

            ClearDirtyDataFlags();
        }

        private void ClearDirtyDataFlags()
        {
            foreach (ApplicationDataType key in this.dirtyData.Keys.ToList())
            {
                this.dirtyData[key] = false;
            }
        }

        private void InitialiseDirtyDataTable()
        {
            foreach (int value in Enum.GetValues(typeof(ApplicationDataType)))
            {
                var enumValue = (ApplicationDataType)value;
                this.dirtyData.Add(enumValue, false);
            }
        }
    }
}