using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class SimplifyClassFieldDeclarations : CodeCleanerCommandRunnerBase, ICodeCleaner
    {
        const int MAX_FIELD_DECLARATION_LENGTH = 80;

        public override SyntaxNode CleanUp(SyntaxNode initialSourceNode)
        {
            return SimplifyClassFieldDeclarationsHelper(initialSourceNode);
        }

        public static SyntaxNode SimplifyClassFieldDeclarationsHelper(SyntaxNode initialSourceNode)
        {
            initialSourceNode = new Rewriter().Visit(initialSourceNode);
            return initialSourceNode;
        }

        class Rewriter : CSharpSyntaxRewriter
        {
            public Rewriter() : base()
            {
            }

            SyntaxTrivia spaceTrivia = SyntaxFactory.Whitespace(" ");
            SyntaxNode Apply(ClassDeclarationSyntax classDescriptionNode)
            {
                var newDeclarationDic = new Dictionary<Type, NewFieldDeclarationDicItem>();

                var fieldDeclarations =
                    classDescriptionNode
                    .Members
                    .OfType<FieldDeclarationSyntax>()
                    .Where(f => (f as FieldDeclarationSyntax).Declaration.Type is PredefinedTypeSyntax)
                    .ToList();

                foreach (var fieldDeclarationItem in fieldDeclarations)
                {
                    var variableType = GetSystemTypeOfTypeNode(fieldDeclarationItem.Declaration);

                    if (newDeclarationDic.ContainsKey(variableType) == false)
                    {
                        newDeclarationDic
                            .Add
                            (
                                variableType,
                                new NewFieldDeclarationDicItem
                                {
                                    VariablesWithoutInitializer = new List<VariableDeclaratorSyntax>(),
                                    VariablesWithInitializer = new List<VariableDeclaratorSyntax>(),
                                    OldFieldDeclarations = new List<FieldDeclarationSyntax>()
                                }
                            );
                    }

                    var currentItem = newDeclarationDic[variableType];

                    currentItem.OldFieldDeclarations.Add(fieldDeclarationItem);

                    var newDeclaration = VisitFieldDeclaration(fieldDeclarationItem) as FieldDeclarationSyntax;

                    currentItem.VariablesWithoutInitializer.AddRange(newDeclaration.Declaration.Variables.Where(v => v.Initializer == null));
                    currentItem.VariablesWithInitializer.AddRange(newDeclaration.Declaration.Variables.Where(v => v.Initializer != null));
                }

                var newDeclarationDicAllItems = newDeclarationDic.ToList();

                newDeclarationDic.Clear();

                foreach (var newDelarationItem in newDeclarationDicAllItems)
                {
                    var finalList = newDelarationItem.Value.VariablesWithoutInitializer.Select(x => x.WithoutTrailingTrivia().WithLeadingTrivia(spaceTrivia)).ToList();
                    finalList.AddRange(newDelarationItem.Value.VariablesWithInitializer.Select(x => x.WithoutTrailingTrivia().WithLeadingTrivia(spaceTrivia)));

                    finalList[0] = finalList[0].WithoutLeadingTrivia();

                    newDelarationItem.Value.NewFieldDeclaration =
                        newDelarationItem.Value.FirstOldFieldDeclarations
                        .WithDeclaration(
                            newDelarationItem.Value.FirstOldFieldDeclarations
                                .Declaration
                                .WithVariables(SyntaxFactory.SeparatedList(finalList))
                        );

                    if (newDelarationItem.Value.NewFieldDeclaration.Span.Length <= MAX_FIELD_DECLARATION_LENGTH)
                    {
                        newDeclarationDic.Add(newDelarationItem.Key, newDelarationItem.Value);
                    }
                    else
                    {
                        foreach (var item in newDelarationItem.Value.OldFieldDeclarations)
                        {
                            fieldDeclarations.Remove(item);
                        }
                    }
                }

                var replaceList = newDeclarationDic.Select(x => x.Value.FirstOldFieldDeclarations).ToList();

                classDescriptionNode =
                    classDescriptionNode
                    .ReplaceNodes
                    (
                         fieldDeclarations,
                         (node1, node2) =>
                         {
                             if (replaceList.Contains(node1))
                             {
                                 var dicItem = newDeclarationDic[GetSystemTypeOfTypeNode((node1 as FieldDeclarationSyntax).Declaration)];

                                 return
                                    dicItem
                                    .NewFieldDeclaration
                                    .WithLeadingTrivia(dicItem.FirstOldFieldDeclarations.GetLeadingTrivia())
                                    .WithTrailingTrivia(dicItem.FirstOldFieldDeclarations.GetTrailingTrivia());
                             }
                             return null;
                         }
                    );

                return classDescriptionNode;
            }
            Type GetSystemTypeOfTypeNode(VariableDeclarationSyntax d)
            {
                return TypesMapItem.GetAllPredefinedTypesDic()[(d.Type as PredefinedTypeSyntax).Keyword.ValueText].BuiltInType;
            }

            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                node = Apply(node) as ClassDeclarationSyntax;

                return node;

            }

            public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
            {
                if (node.Initializer == null) return base.VisitVariableDeclarator(node);
                if (node.Parent is VariableDeclarationSyntax == false) return base.VisitVariableDeclarator(node);
                var variableTypeNode = (node.Parent as VariableDeclarationSyntax).Type;
                var value = node.Initializer.Value;

                if (value is LiteralExpressionSyntax)
                {
                    var valueObj = (value as LiteralExpressionSyntax).Token.Value;
                    var typeItem = TypesMapItem.GetAllPredefinedTypesDic()[(variableTypeNode as PredefinedTypeSyntax).Keyword.ValueText];

                    if ((typeItem.DefaultValue == null && valueObj != null) || (typeItem.DefaultValue != null && !typeItem.DefaultValue.Equals(valueObj))) return base.VisitVariableDeclarator(node);

                    node = node.WithInitializer(null).WithoutTrailingTrivia();
                }
                //else if (value is DefaultExpressionSyntax)
                //{
                //    node = node.WithInitializer(null).WithoutTrailingTrivia();
                //}
                //else if (value is ObjectCreationExpressionSyntax)
                //{
                //    if (variableTypeNode.IsKind(SyntaxKind.PredefinedType))
                //    {
                //        node = node.WithInitializer(null).WithoutTrailingTrivia();
                //    }
                //}

                return base.VisitVariableDeclarator(node);
            }

            class NewFieldDeclarationDicItem
            {
                public List<VariableDeclaratorSyntax> VariablesWithoutInitializer { get; set; }
                public List<VariableDeclaratorSyntax> VariablesWithInitializer { get; set; }
                public FieldDeclarationSyntax FirstOldFieldDeclarations
                {
                    get
                    {
                        return OldFieldDeclarations.First();
                    }
                }
                public List<FieldDeclarationSyntax> OldFieldDeclarations { get; set; }
                public FieldDeclarationSyntax NewFieldDeclaration { get; set; }
            }
        }
    }
}