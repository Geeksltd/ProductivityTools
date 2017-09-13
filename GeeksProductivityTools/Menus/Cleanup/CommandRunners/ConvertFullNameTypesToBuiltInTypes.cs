using System;
using System.Linq;
using System.CodeDom;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CSharp;
using EnvDTE;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class ConvertFullNameTypesToBuiltInTypes : ICodeCleaner
    {
        public void Run(ProjectItem item) => Task.Run(() => ConvertTypeNames(item));

        class TypesMapItem
        {
            public string Name { get; set; }
            public string FullName { get; set; }
            public string BuiltInName { get; set; }
            public TypeSyntax NewNode { get; set; }
        }
        static TypesMapItem GetBuiltInTypes(Type type, TypeSyntax node, CSharpCodeProvider provider)
        {
            return new TypesMapItem
            {
                Name = type.Name,
                FullName = type.FullName,
                BuiltInName = provider.GetTypeOutput(new CodeTypeReference(type)),
                NewNode = node
            };
        }
        static Dictionary<string, TypesMapItem> GetBuiltInTypesDic()
        {
            var output = new Dictionary<string, TypesMapItem>();

            using (var provider = new CSharpCodeProvider())
            {
                var typesList = new TypesMapItem[]
                {
                    GetBuiltInTypes(typeof(Boolean), GetPredefineType(SyntaxKind.BoolKeyword), provider),
                    GetBuiltInTypes(typeof(Byte),GetPredefineType(SyntaxKind.ByteKeyword), provider),
                    GetBuiltInTypes(typeof(SByte),GetPredefineType(SyntaxKind.SByteKeyword), provider),
                    GetBuiltInTypes(typeof(Char),GetPredefineType(SyntaxKind.CharKeyword), provider),
                    GetBuiltInTypes(typeof(Decimal),GetPredefineType(SyntaxKind.DecimalKeyword), provider),
                    GetBuiltInTypes(typeof(Double),GetPredefineType(SyntaxKind.DoubleKeyword), provider),
                    GetBuiltInTypes(typeof(Single),GetPredefineType(SyntaxKind.FloatKeyword), provider),
                    GetBuiltInTypes(typeof(Int32),GetPredefineType(SyntaxKind.IntKeyword), provider),
                    GetBuiltInTypes(typeof(UInt32),GetPredefineType(SyntaxKind.UIntKeyword), provider),
                    GetBuiltInTypes(typeof(Int64),GetPredefineType(SyntaxKind.LongKeyword), provider),
                    GetBuiltInTypes(typeof(UInt64),GetPredefineType(SyntaxKind.ULongKeyword), provider),
                    GetBuiltInTypes(typeof(Object),GetPredefineType(SyntaxKind.ObjectKeyword), provider),
                    GetBuiltInTypes(typeof(Int16),GetPredefineType(SyntaxKind.ShortKeyword), provider),
                    GetBuiltInTypes(typeof(UInt16),GetPredefineType(SyntaxKind.UShortKeyword), provider),
                    GetBuiltInTypes(typeof(String),GetPredefineType(SyntaxKind.StringKeyword), provider),
                };

                foreach (var item in typesList)
                {
                    output.Add(item.Name, item);
                    output.Add(item.FullName, item);
                }

                return output;
            }
        }

        private static TypeSyntax GetPredefineType(SyntaxKind keyword)
        {
            return SyntaxFactory.PredefinedType(SyntaxFactory.Token(keyword));
        }

        static void ConvertTypeNames(ProjectItem item)
        {
            var initialSource = item.ToSyntaxNode();

            initialSource = ConvertTypeNames(initialSource);

            initialSource.WriteSourceTo(item.ToFullPathPropertyValue());
        }
        static SyntaxNode ConvertTypeNames(SyntaxNode initialSource)
        {
            var builtInTypesMapDic = GetBuiltInTypesDic();

            var selectedTokensList =
                initialSource
                .DescendantNodes()
                .Select(node =>
                    new
                    {
                        Node = node,
                        NodeText = node.WithoutTrivia().ToFullString()
                    })
                .Where(n => builtInTypesMapDic.ContainsKey(n.NodeText))
                .Select(n => n.Node);
                             
            return initialSource.ReplaceNodes(
                    selectedTokensList,
                    (oldNode1, oldNode2) =>
                    {
                        if (oldNode1.Parent is QualifiedNameSyntax) return oldNode1;
                        if (oldNode1.Parent is MemberAccessExpressionSyntax)
                        {
                            if ((oldNode1.Parent as MemberAccessExpressionSyntax).Expression != oldNode1) return oldNode1;
                        }
                        else if ((oldNode1 is IdentifierNameSyntax) == false && (oldNode1 is QualifiedNameSyntax) == false) return oldNode1;

                        return
                            builtInTypesMapDic[oldNode1.WithoutTrivia().ToFullString()]
                                .NewNode
                                .WithLeadingTrivia(oldNode1.GetLeadingTrivia())
                                .WithTrailingTrivia(oldNode1.GetTrailingTrivia());
                    }
                );
        }
    }
}