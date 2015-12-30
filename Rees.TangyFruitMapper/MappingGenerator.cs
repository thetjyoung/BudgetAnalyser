﻿using System;
using System.Linq;
using JetBrains.Annotations;

namespace Rees.TangyFruitMapper
{
    /// <summary>
    ///     A Convention based C# code generator for mapping a model object to a DTO object and back.
    ///     This class is designed to be used either with T4, console application, or a unit test.
    /// </summary>
    public class MappingGenerator
    {
        private Action<string> codeOutput;
        private Type dtoType;
        private int indent;
        private Type modelType;
        private NamespaceFinder namespaceFinder;

        /// <summary>
        ///     An optional delegate to a logging action to output diagnostic messages for debugging and troubleshooting purposes.
        /// </summary>
        public Action<string> DiagnosticLogging { get; set; }

        /// <summary>
        ///     Generates the code for the specified types. Be sure to check for TODO's in the generated code.
        /// </summary>
        /// <typeparam name="TDto">The type of the dto. It is important that this Dto follows the Dto conventions.</typeparam>
        /// <typeparam name="TModel">The type of the model. There are less convention rules for model objects.</typeparam>
        /// <param name="codeOutputDelegate">An action to output the code.</param>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public void Generate<TDto, TModel>([NotNull] Action<string> codeOutputDelegate)
        {
            if (codeOutputDelegate == null) throw new ArgumentNullException(nameof(codeOutputDelegate));
            if (DiagnosticLogging == null) DiagnosticLogging = x => { };

            this.codeOutput = codeOutputDelegate;
            this.modelType = typeof(TModel);
            this.dtoType = typeof(TDto);
            this.namespaceFinder = new NamespaceFinder(this.dtoType, this.modelType);
            DiagnosticLogging($"Starting to generate code for mapping {this.dtoType.Name} to {this.modelType.Name}...");

            MapByProperties.ClearMapCache();
            var mapper = new MapByProperties(DiagnosticLogging, this.dtoType, this.modelType);
            var mapResult = mapper.CreateMap();

            WriteFileHeader();

            WriteMappingClasses(mapResult);

            WriteFileFooter();

            DiagnosticLogging($"================== Mapping Complete {this.dtoType.Name} to {this.modelType.Name} ======================");
        }

        private string Indent(bool increment = false)
        {
            if (increment) this.indent++;
            return new string(' ', 4*this.indent);
        }

        private string Outdent()
        {
            this.indent--;
            if (this.indent < 0) this.indent = 0;
            return new string(' ', 4*this.indent);
        }

        private void WriteClassFooter()
        {
            this.codeOutput($@"{Outdent()}}} // End Class
");
        }

        private void WriteClassHeader(MapResult map)
        {
            this.codeOutput(
                $@"{Indent()}[GeneratedCode(""1.0"", ""Tangy Fruit Mapper"")]
{Indent()}public class {map.MapperName} : IDtoMapper<{map.DtoType.Name}, {map.ModelType.Name}>
{Indent()}{{
{
                    Indent(true)}");
        }

        private void WriteFileFooter()
        {
            this.codeOutput($@"{Outdent()}}} // End Namespace");
        }

        private void WriteFileHeader()
        {
            this.codeOutput($@"using System.CodeDom.Compiler;
using System.Linq;
using System.Reflection;
using Rees.TangyFruitMapper;");
            foreach (var ns in this.namespaceFinder.DiscoverNamespaces())
            {
                this.codeOutput($@"using {ns.Key};");
            }
            this.codeOutput($@"
namespace GeneratedCode
{{
{Indent(true)}");
        }

        private void WriteMappingClasses(MapResult mapResult)
        {
            WriteClassHeader(mapResult);
            WriteMethods(mapResult);
            WriteClassFooter();

            if (mapResult.DependentOnMaps.Any())
            {
                foreach (var nestedMap in mapResult.DependentOnMaps)
                {
                    WriteMappingClasses(nestedMap);
                }
            }
        }

        private void WriteMethods(MapResult map)
        {
            // TODO maybe add support for internal constructors? The below code assumes public default constructors are available.
            this.codeOutput(
                $@"{Indent()}public {map.ModelType.Name} ToModel({map.DtoType.Name} {AssignmentStrategy.DtoVariableName})
{Indent()}{{
{Indent(true)}var {AssignmentStrategy.ModelVariableName
                    } = new {map.ModelType.Name}();
{Indent()}var {AssignmentStrategy.ModelTypeVariableName} = {AssignmentStrategy.ModelVariableName}.GetType();");
            foreach (var assignment in map.ModelToDtoMap.Values)
            {
                // model.Property = dto.Property;
                this.codeOutput($"{Indent()}{assignment.Source.CreateCodeLine(DtoOrModel.Dto)}");
                this.codeOutput($"{Indent()}{assignment.Destination.CreateCodeLine(DtoOrModel.Model, assignment.Source.SourceVariableName)}");
            }
            this.codeOutput($@"{Indent()}return {AssignmentStrategy.ModelVariableName};
{Outdent()}}} // End ToModel Method");


            this.codeOutput(
                $@"
{Indent()}public {map.DtoType.Name} ToDto({map.ModelType.Name} {AssignmentStrategy.ModelVariableName})
{Indent()}{{
{Indent(true)}var {AssignmentStrategy.DtoVariableName
                    } = new {map.DtoType.Name}();");
            foreach (var assignment in map.DtoToModelMap.Values)
            {
                this.codeOutput($"{Indent()}{assignment.Source.CreateCodeLine(DtoOrModel.Model)}");
                this.codeOutput($"{Indent()}{assignment.Destination.CreateCodeLine(DtoOrModel.Dto, assignment.Source.SourceVariableName)}");
            }
            this.codeOutput($@"{Indent()}return {AssignmentStrategy.DtoVariableName};
{Outdent()}}} // End ToDto Method");
        }
    }
}