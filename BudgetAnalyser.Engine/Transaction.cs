﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BudgetAnalyser.Engine.Account;
using BudgetAnalyser.Engine.Budget;

namespace BudgetAnalyser.Engine
{
    [DebuggerDisplay("{Date} {Amount} {Description} {BudgetBucket}")]
    public class Transaction : INotifyPropertyChanged, IComparable
    {
        public Transaction()
        {
            Id = Guid.NewGuid();
        }

        private BudgetBucket budgetBucket;

        public event PropertyChangedEventHandler PropertyChanged;
        
        public AccountType AccountType { get; set; }
        
        public decimal Amount { get; set; }

        public BudgetBucket BudgetBucket
        {
            get { return this.budgetBucket; }

            set
            {
                this.budgetBucket = value;
                this.OnPropertyChanged();
            }
        }

        public DateTime Date { get; set; }

        public string Description { get; set; }

        public Guid Id { get; set; }

        public string Reference1 { get; set; }

        public string Reference2 { get; set; }

        public string Reference3 { get; set; }

        public bool IsSuspectedDuplicate { get; internal set; }

        public TransactionType TransactionType { get; set; }

        public int CompareTo(object obj)
        {
            var otherTransaction = obj as Transaction;
            if (otherTransaction == null)
            {
                return 1;
            }

            return this.Date.CompareTo(otherTransaction.Date);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = 37; // prime
                result += AccountType.GetType().GetHashCode();
                result *= 397; // also prime 
                result += Amount.GetHashCode();
                result *= 397;
                result += Date.GetHashCode();
                result *= 397;
                
                if (!string.IsNullOrWhiteSpace(Description))
                    result += Description.GetHashCode();
                result *= 397;

                if (!string.IsNullOrWhiteSpace(Reference1))
                    result += Reference1.GetHashCode();
                result *= 397;

                if (!string.IsNullOrWhiteSpace(Reference2))
                    result += Reference2.GetHashCode();
                result *= 397;

                if (!string.IsNullOrWhiteSpace(Reference3))
                    result += Reference3.GetHashCode();
                result *= 397;

                result += TransactionType.GetHashCode();
                result *= 397;

                return result;
            }
        }
    }
}