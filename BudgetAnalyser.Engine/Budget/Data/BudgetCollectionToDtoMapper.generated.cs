﻿using System.CodeDom.Compiler;
using System.Linq;
using System.Reflection;
using Rees.TangyFruitMapper;
using BudgetAnalyser.Engine.Budget;
using System.Collections.Generic;
using System;
using BudgetAnalyser.Engine.Budget.Data;

namespace GeneratedCode
{
    [GeneratedCode("1.0", "Tangy Fruit Mapper 2/01/2016 12:26:26 AM UTC")]
    internal partial class Mapper_BudgetCollectionDto_BudgetCollection : IDtoMapper<BudgetCollectionDto, BudgetCollection>
    {

        public virtual BudgetCollection ToModel(BudgetCollectionDto dto)
        {
            ToModelPreprocessing(dto);
            BudgetCollection model = null;
            ModelFactory(dto, ref model);
            if (model == null) model = new BudgetCollection();
            var modelType = model.GetType();
            var storageKey1 = dto.StorageKey;
            model.StorageKey = storageKey1;
            ToModelPostprocessing(dto, ref model);
            return model;
        } // End ToModel Method

        public virtual BudgetCollectionDto ToDto(BudgetCollection model)
        {
            ToDtoPreprocessing(model);
            BudgetCollectionDto dto;
            dto = new BudgetCollectionDto();
            // var buckets2 = // TODO Cannot find a way to retrieve this property: model.Buckets. 
            // dto.Buckets = buckets2; // TODO Cannot find a way to set this property: dto.Buckets. 
            // var budgets3 = // TODO Cannot find a way to retrieve this property: model.Budgets. 
            // dto.Budgets = budgets3; // TODO Cannot find a way to set this property: dto.Budgets. 
            var storageKey4 = model.StorageKey;
            dto.StorageKey = storageKey4;
            ToDtoPostprocessing(ref dto, model);
            return dto;
        } // End ToDto Method
        partial void ToModelPreprocessing(BudgetCollectionDto dto);
        partial void ToDtoPreprocessing(BudgetCollection model);
        partial void ModelFactory(BudgetCollectionDto dto, ref BudgetCollection model);
        partial void ToModelPostprocessing(BudgetCollectionDto dto, ref BudgetCollection model);
        partial void ToDtoPostprocessing(ref BudgetCollectionDto dto, BudgetCollection model);
    } // End Class
} // End Namespace
