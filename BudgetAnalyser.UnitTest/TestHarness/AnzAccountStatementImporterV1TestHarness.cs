﻿using System;
using System.Collections.Generic;
using BudgetAnalyser.Engine;
using BudgetAnalyser.Engine.Annotations;
using BudgetAnalyser.Engine.Statement;
using Rees.UserInteraction.Contracts;

namespace BudgetAnalyser.UnitTest.TestHarness
{
    public class AnzAccountStatementImporterV1TestHarness : AnzAccountStatementImporterV1
    {
        public AnzAccountStatementImporterV1TestHarness([NotNull] IUserMessageBox userMessageBox, [NotNull] BankImportUtilities importUtilities)
            : base(userMessageBox, importUtilities, new FakeLogger())
        {
        }

        public Func<string, IEnumerable<string>> ReadLinesOverride { get; set; }

        public Func<string, string> ReadTextChunkOverride { get; set; }

        protected override IEnumerable<string> ReadLines(string fileName)
        {
            if (ReadLinesOverride == null)
            {
                return new List<string>();
            }

            return ReadLinesOverride(fileName);
        }

        protected override string ReadTextChunk(string filePath)
        {
            if (ReadTextChunkOverride == null)
            {
                return "Atm Debit,Anz  1234567 Queen St,Anz  S3A1234,Queen St,Anch  123456,-80.00,16/06/2014,";
            }

            return ReadTextChunkOverride(filePath);
        }
    }
}