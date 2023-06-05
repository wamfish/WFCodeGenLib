//  Copyright (C) 2023 - Present John Roscoe Hamilton - All Rights Reserved
//  You may use, distribute and modify this code under the terms of the MIT license.
//  See the file License.txt in the root folder for full license details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace WFCodeGenLib
{
    /// <summary>
    /// Creates a List<> of all ClassDeclarationSyntax nodes
    /// </summary>
    class ClassSyntaxReceiver : ISyntaxReceiver
    {
        public readonly StringBuilder AllClasses = new StringBuilder();
        public readonly List<ClassDeclarationSyntax> Classes = new List<ClassDeclarationSyntax>();
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (!(syntaxNode is ClassDeclarationSyntax)) return;
            var c = (ClassDeclarationSyntax)syntaxNode;
            AllClasses.AppendLine(c.Identifier.ToString());

            if (c.BaseList == null) return;
            var sbt = c.BaseList.ChildNodes().OfType<SimpleBaseTypeSyntax>().First();
            var insNodes = sbt.ChildNodes().OfType<IdentifierNameSyntax>();
            if (insNodes.Count() > 0)
            {
                var ins = insNodes.First();
                if (ins.Identifier.ValueText == "Record") Classes.Add(c);
            }
        }
    }
}