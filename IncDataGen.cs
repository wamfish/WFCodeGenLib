//  Copyright (C) 2023 - Present John Roscoe Hamilton - All Rights Reserved
//  You may use, distribute and modify this code under the terms of the MIT license.
//  See the file License.txt in the root folder for full license details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace WFCodeGenLib
{

    /// <summary>
    /// The main class for generating source code for any class with a base of RecData. 
    /// </summary>

    [Generator]

    public partial class IncDataGen : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
#if DEBUG
            //Console.WriteLine("Test");
            // Uncomment to debug code generation:
            //if (!System.Diagnostics.Debugger.IsAttached)
            //{
            //    System.Diagnostics.Debugger.Launch();
            //}
#endif

            IncrementalValuesProvider<ClassDeclarationSyntax> classes;
            classes = context.SyntaxProvider.CreateSyntaxProvider
            (
                (s, _) => IsSyntaxTargetForGeneration(s),
                (ctx, _) => GetTargetForGeneration(ctx)
            );
            IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses = context.CompilationProvider.Combine(classes.Collect());
            context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }
        static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax;
        static ClassDeclarationSyntax GetTargetForGeneration(GeneratorSyntaxContext context)
        {
            var cds = (ClassDeclarationSyntax)context.Node;
            if (cds.BaseList == null) return null;
            var sbt = cds.BaseList.ChildNodes().OfType<SimpleBaseTypeSyntax>().First();
            var insNodes = sbt.ChildNodes().OfType<IdentifierNameSyntax>();
            if (insNodes.Count() > 0)
            {
                var ins = insNodes.First();
                if (ins.Identifier.ValueText == "Record") return cds;
                if (ins.Identifier.ValueText == "Data" && cds.Identifier.ToString() != "Record") return cds;
            }
            return null;
        }
        static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            try
            {
                ClassInfo.KAttribute = compilation.GetTypeByMetadataName("WFLib.KAttribute");
                ClassInfo.XAttribute = compilation.GetTypeByMetadataName("WFLib.XAttribute");
                ClassInfo.RangeAttribute = compilation.GetTypeByMetadataName("WFLib.RangeAttribute");
                foreach (var c in classes)
                {
                    if (c == null) continue;
                    var src = Generator.Generate(compilation, c, out string className);
                    SourceText sourceText = src.Source();
                    context.AddSource($"{className}.gen.cs", sourceText);
                }
            }
            catch (Exception ex)
            {
                string st = ex.StackTrace.Replace("\n", " ");
                st = st.Replace("\r", "");
                var descriptor = new DiagnosticDescriptor(
                id: "WFLIBGC001",
                title: "Unexpected Error",
                messageFormat: $"Error for object: {0} {ex.Message} {st}",
                category: "Design",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);
                context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, $"{ex.Message}{ex.StackTrace}"));
            }
        }
    }
}