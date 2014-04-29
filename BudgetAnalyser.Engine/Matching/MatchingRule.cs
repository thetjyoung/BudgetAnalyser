﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BudgetAnalyser.Engine.Annotations;
using BudgetAnalyser.Engine.Budget;
using BudgetAnalyser.Engine.Statement;

namespace BudgetAnalyser.Engine.Matching
{
    [DebuggerDisplay("Rule: {Description} {RuleId} {BucketCode")]
    public class MatchingRule : INotifyPropertyChanged, IEquatable<MatchingRule>
    {
        private readonly IBudgetBucketRepository bucketRepository;
        private decimal? doNotUseAmount;
        private string doNotUseDescription;
        private DateTime? doNotUseLastMatch;
        private int doNotUseMatchCount;
        private string doNotUseReference1;
        private string doNotUseReference2;
        private string doNotUseReference3;
        private string doNotUseTransactionType;

        /// <summary>
        ///     Used any other time.
        /// </summary>
        /// <param name="bucketRepository"></param>
        public MatchingRule([NotNull] IBudgetBucketRepository bucketRepository)
        {
            if (bucketRepository == null)
            {
                throw new ArgumentNullException("bucketRepository");
            }

            this.bucketRepository = bucketRepository;
            RuleId = Guid.NewGuid();
            Created = DateTime.Now;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public decimal? Amount
        {
            get { return this.doNotUseAmount; }
            set
            {
                this.doNotUseAmount = value == 0 ? null : value;
                OnPropertyChanged();
            }
        }

        public BudgetBucket Bucket
        {
            get { return this.bucketRepository.GetByCode(BucketCode); }

            set
            {
                if (value == null)
                {
                    BucketCode = null;
                    return;
                }

                BucketCode = value.Code;
                OnPropertyChanged();
            }
        }

        public DateTime Created { get; internal set; }

        public string Description
        {
            get { return this.doNotUseDescription; }
            set
            {
                this.doNotUseDescription = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets the last date and time the rule was matched to a transaction.
        /// </summary>
        public DateTime? LastMatch
        {
            get { return this.doNotUseLastMatch; }
            internal set
            {
                this.doNotUseLastMatch = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets the number of times this rule has been matched to a transaction.
        /// </summary>
        public int MatchCount
        {
            get { return this.doNotUseMatchCount; }
            internal set
            {
                this.doNotUseMatchCount = value;
                OnPropertyChanged();
            }
        }

        public string Reference1
        {
            get { return this.doNotUseReference1; }

            set
            {
                this.doNotUseReference1 = value == null ? null : value.Trim();
                OnPropertyChanged();
            }
        }

        public string Reference2
        {
            get { return this.doNotUseReference2; }

            set
            {
                this.doNotUseReference2 = value == null ? null : value.Trim();
                OnPropertyChanged();
            }
        }

        public string Reference3
        {
            get { return this.doNotUseReference3; }

            set
            {
                this.doNotUseReference3 = value == null ? null : value.Trim();
                OnPropertyChanged();
            }
        }

        public Guid RuleId { get; internal set; }

        public string TransactionType
        {
            get { return this.doNotUseTransactionType; }
            set
            {
                this.doNotUseTransactionType = value;
                OnPropertyChanged();
            }
        }

        internal string BucketCode { get; set; }

        public static bool operator ==(MatchingRule left, MatchingRule right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MatchingRule left, MatchingRule right)
        {
            return !Equals(left, right);
        }

        public bool Equals(MatchingRule other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return RuleId.Equals(other.RuleId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((MatchingRule)obj);
        }

        public override int GetHashCode()
        {
            return RuleId.GetHashCode();
        }

        public bool Match(Transaction transaction)
        {
            bool matched = false;
            if (!string.IsNullOrWhiteSpace(Description))
            {
                if (transaction.Description == Description)
                {
                    matched = true;
                }
            }

            if (!matched && !string.IsNullOrWhiteSpace(Reference1))
            {
                if (transaction.Reference1 == Reference1)
                {
                    matched = true;
                }
            }

            if (!matched && !string.IsNullOrWhiteSpace(Reference2))
            {
                if (transaction.Reference2 == Reference2)
                {
                    matched = true;
                }
            }

            if (!matched && !string.IsNullOrWhiteSpace(Reference3))
            {
                if (transaction.Reference3 == Reference3)
                {
                    matched = true;
                }
            }

            if (!matched && !string.IsNullOrWhiteSpace(TransactionType))
            {
                if (transaction.TransactionType.Name == TransactionType)
                {
                    matched = true;
                }
            }

            if (matched)
            {
                LastMatch = DateTime.Now;
                MatchCount++;
            }

            return matched;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}