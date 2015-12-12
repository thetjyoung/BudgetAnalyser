using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BudgetAnalyser.Engine.Annotations;

namespace BudgetAnalyser.Engine.Ledger
{
    /// <summary>
    ///     This class is responsible for validating that changes were made during a reconciliation that did not alter the
    ///     Ledger Book in an inappropriate and invalid way.
    /// </summary>
    public sealed class ReconciliationConsistencyChecker : IDisposable
    {
        private readonly decimal check1;

        private readonly LedgerBook ledgerBook;
        private decimal check2;

        public ReconciliationConsistencyChecker([NotNull] LedgerBook book)
        {
            if (book == null)
            {
                throw new ArgumentNullException(nameof(book));
            }

            this.ledgerBook = book;
            this.check1 = this.ledgerBook.Reconciliations.Sum(e => e.CalculatedSurplus);
        }

        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "Allowed here, using syntax only")]
        [SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly", Justification = "Not required here, using syntax only")]
        public void Dispose()
        {
            this.check2 = this.ledgerBook.Reconciliations.Sum(e => e.CalculatedSurplus) - this.ledgerBook.Reconciliations.First().CalculatedSurplus;
            if (this.check1 != this.check2)
            {
                throw new CorruptedLedgerBookException("Code Error: The previous dated entries have changed, this is not allowed. Data is corrupt.");
            }
        }
    }
}