﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace BudgetAnalyser.Engine.Ledger.Data
{
    /// <summary>
    /// A Dto for <see cref="LedgerBook"/>
    /// </summary>
    public class LedgerBookDto
    {
        public LedgerBookDto()
        {
            DatedEntries = new List<LedgerEntryLineDto>();
            Ledgers = new List<LedgerColumnDto>();
        }

        /// <summary>
        /// A hash value to verify once loaded from a file.
        /// </summary>
        public double Checksum { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Required for serialisation")]
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "Required for serialisation")]
        public List<LedgerEntryLineDto> DatedEntries { get; set; }

        public string FileName { get; set; }

        /// <summary>
        /// The ledger to Bucket mapping for when a new reconciliation creates a new instances of LedgerEntry's.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Required for serialisation")]
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Scope = "member", Target = "BudgetAnalyser.Engine.Ledger.Data.LedgerBookDto.#Ledgers", Justification = "Required for serialisation")]
        public List<LedgerColumnDto> Ledgers { get; set; }

        public DateTime Modified { get; set; }
        public string Name { get; set; }
    }
}