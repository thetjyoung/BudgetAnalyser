﻿using System;
using System.Linq;
using System.Xaml;
using BudgetAnalyser.Engine.Annotations;

namespace BudgetAnalyser.Engine.Ledger
{
    [AutoRegisterWithIoC(SingleInstance = true)]
    public class LedgerBookRepository : ILedgerBookRepository
    {
        private readonly ILedgerDataToDomainMapper dataToDomainMapper;
        private readonly ILedgerDomainToDataMapper domainToDataMapper;

        public LedgerBookRepository([NotNull] ILedgerDataToDomainMapper dataToDomainMapper, [NotNull] ILedgerDomainToDataMapper domainToDataMapper)
        {
            if (dataToDomainMapper == null)
            {
                throw new ArgumentNullException("dataToDomainMapper");
            }

            if (domainToDataMapper == null)
            {
                throw new ArgumentNullException("domainToDataMapper");
            }

            this.dataToDomainMapper = dataToDomainMapper;
            this.domainToDataMapper = domainToDataMapper;
        }

        public bool Exists(string fileName)
        {
            return System.IO.File.Exists(fileName);
        }

        public LedgerBook Load(string fileName)
        {
            var dataEntity = XamlServices.Load(fileName) as DataLedgerBook;
            if (dataEntity == null)
            {
                throw new FileFormatException(string.Format("The specified file {0} is not of type DataLedgerBook", fileName));
            }

            if (dataEntity.Checksum == null)
            {
                // bypass checksum check
            }
            else
            {
                var calculatedChecksum = CalculateChecksum(dataEntity);
                if (calculatedChecksum != dataEntity.Checksum)
                {
                    throw new FileFormatException("The Ledger Book has been tampered with, checksum should be " + calculatedChecksum);
                }
            }

            return this.dataToDomainMapper.Map(dataEntity);
        }

        public void Save(LedgerBook book)
        {
            Save(book, book.FileName);
        }

        public void Save(LedgerBook book, string fileName)
        {
            var dataEntity = this.domainToDataMapper.Map(book);
            dataEntity.FileName = fileName;
            dataEntity.Checksum = CalculateChecksum(dataEntity);

            XamlServices.Save(dataEntity.FileName, dataEntity);
        }

        private double CalculateChecksum(DataLedgerBook dataEntity)
        {
            unchecked
            {
                return dataEntity.DatedEntries.Sum(l => 
                    (double)l.BankBalance 
                    + l.BankBalanceAdjustments.Sum(b => (double)b.Credit - (double)b.Debit)
                    + l.Entries.Sum(e => (double)e.Balance));
            }
        }
    }
}