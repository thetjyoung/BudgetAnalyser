﻿using System;
using System.Collections.Generic;
using System.Linq;
using BudgetAnalyser.Engine;
using BudgetAnalyser.Engine.Budget;
using BudgetAnalyser.Engine.Ledger;
using BudgetAnalyser.Engine.Ledger.Data;

namespace BudgetAnalyser.UnitTest.Helper
{
    public static class LedgerBookHelper
    {
        public static Dictionary<BudgetBucket, int> LedgerOrder(LedgerBook book)
        {
            var ledgerOrder = new Dictionary<BudgetBucket, int>();
            var index = 0;
            foreach (LedgerBucket ledger in book.Ledgers.OrderBy(l => l.BudgetBucket))
            {
                Console.Write("{0}", ledger.BudgetBucket.Code.PadRight(20));
                ledgerOrder.Add(ledger.BudgetBucket, index++);
            }
            return ledgerOrder;
        }

        public static void Output(this LedgerBook book, bool outputTransactions = false)
        {
            Console.WriteLine("Name: {0}", book.Name);
            Console.WriteLine("Filename: {0}", book.StorageKey);
            Console.WriteLine("Modified: {0}", book.Modified);
            Console.Write("Date        ");
            Dictionary<BudgetBucket, int> ledgerOrder = LedgerOrder(book);

            OutputReconciliationHeader();

            foreach (LedgerEntryLine line in book.Reconciliations)
            {
                Output(line, ledgerOrder, outputTransactions);
            }
        }

        public static void Output(this LedgerEntryLine line, Dictionary<BudgetBucket, int> ledgerOrder, bool outputTransactions = false, bool outputHeader = false)
        {
            if (outputHeader)
            {
                OutputReconciliationHeader();
            }

            Console.Write("{0:d}  ", line.Date);
            foreach (LedgerEntry entry in line.Entries.OrderBy(e => e.LedgerBucket.BudgetBucket))
            {
                Console.Write("{0} {1} {2}", entry.NetAmount.ToString("N").PadRight(8), entry.LedgerBucket.StoredInAccount.Name.Truncate(1), entry.Balance.ToString("N").PadRight(9));
            }

            Console.Write(line.CalculatedSurplus.ToString("N").PadRight(9));
            var balanceCount = 0;
            foreach (BankBalance bankBalance in line.BankBalances)
            {
                if (++balanceCount > 2)
                {
                    break;
                }
                // Only two bank balances are shown in the test output at this stage.
                string balanceText = String.Format("{0} {1} ", bankBalance.Account.Name.Truncate(1), bankBalance.Balance.ToString("N"));
                Console.Write(balanceText.PadLeft(13).TruncateLeft(13));
            }
            Console.Write(line.TotalBalanceAdjustments.ToString("N").PadLeft(9).TruncateLeft(9));
            Console.Write(line.LedgerBalance.ToString("N").PadLeft(13).TruncateLeft(13));
            Console.WriteLine();

            if (outputTransactions)
            {
                foreach (LedgerEntry entry in line.Entries.OrderBy(e => e.LedgerBucket.BudgetBucket))
                {
                    var tab = new string(' ', 11 + 18 * ledgerOrder[entry.LedgerBucket.BudgetBucket]);
                    foreach (LedgerTransaction transaction in entry.Transactions)
                    {
                        Console.WriteLine(
                            "{0} {1} {2} {3} {4} {5}",
                            tab,
                            entry.LedgerBucket.BudgetBucket.Code.PadRight(6),
                            transaction.Amount >= 0 ? (transaction.Amount.ToString("N") + "Cr").PadLeft(8) : (transaction.Amount.ToString("N") + "Dr").PadLeft(16),
                            transaction.Narrative.Truncate(15),
                            transaction.Id,
                            transaction.AutoMatchingReference);
                    }
                }
                Console.WriteLine("=================================================================================================================================");
            }
        }

        public static void Output(this LedgerBookDto book, bool outputTransactions = false)
        {
            Console.WriteLine("Name: {0}", book.Name);
            Console.WriteLine("Filename: {0}", book.StorageKey);
            Console.WriteLine("Modified: {0}", book.Modified);
            Console.Write("Date        ");
            foreach (string ledger in book.Reconciliations.SelectMany(l => l.Entries.Select(e => e.BucketCode)))
            {
                Console.Write("{0}", ledger.PadRight(18));
            }

            Console.Write("Surplus  BankBalance  Adjustments LedgerBalance");
            Console.WriteLine();
            Console.WriteLine("====================================================================================================================");

            foreach (LedgerEntryLineDto line in book.Reconciliations)
            {
                Console.Write("{0:d}  ", line.Date);
                foreach (LedgerEntryDto entry in line.Entries)
                {
                    Console.Write("{0} {1} ", new string(' ', 8), entry.Balance.ToString("N").PadRight(8));
                }

                Console.Write(line.BankBalance.ToString("N").PadRight(13));
                Console.Write(line.BankBalanceAdjustments.Sum(t => t.Amount).ToString("N").PadRight(12));
                Console.Write(line.BankBalances.Sum(b => b.Balance).ToString("N").PadRight(9));
                Console.WriteLine();

                if (outputTransactions)
                {
                    foreach (LedgerEntryDto entry in line.Entries)
                    {
                        foreach (LedgerTransactionDto transaction in entry.Transactions)
                        {
                            Console.WriteLine(
                                "          {0} {1} {2}",
                                entry.BucketCode.PadRight(6),
                                transaction.Amount > 0 ? (transaction.Amount.ToString("N") + "Cr").PadLeft(8) : (transaction.Amount.ToString("N") + "Dr").PadLeft(16),
                                transaction.Narrative);
                        }
                    }
                    Console.WriteLine("====================================================================================================================");
                }
            }
        }

        private static void OutputReconciliationHeader()
        {
            Console.Write("Surplus    BankBalances    Adjusts LedgerBalance");
            Console.WriteLine();
            Console.WriteLine("==============================================================================================================================================");
        }

        public static void Output(this LedgerEntry instance)
        {
            Console.WriteLine($"Ledger Entry Transactions. ============================================");
            foreach (LedgerTransaction transaction in instance.Transactions)
            {
                Console.WriteLine($"{transaction.Date:d} {transaction.Narrative} {transaction.Amount:F2}");
            }
            Console.WriteLine("----------------------------------------------------------------------------------------");
            Console.WriteLine($"{instance.Transactions.Count()} transactions. NetAmount: {instance.NetAmount:F2} ClosingBalance: {instance.Balance:F2}");
        }
    }
}