using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    // TODO: Refactor this bloody class Ali
    public class WhiteSpaceNormalizer : ICodeCleaner
    {
        public void Run(ProjectItem item) => Task.Run(() => NormalizeWhiteSpace(item));

        static void NormalizeWhiteSpace(ProjectItem item)
        {
            var initialSource = item.ToSyntaxNode();

            initialSource = RevomeDuplicaterBlank(initialSource);

            initialSource.WriteSourceTo(item.ToFullPathPropertyValue());
        }
        //public static void NormalizeWhiteSpace(string address)
        //{
        //    var initialSource = CSharpSyntaxTree.ParseText(File.ReadAllText(address)).GetRoot();

        //    initialSource = RevomeDuplicaterBlank(initialSource);

        //    initialSource.WriteSourceTo(address);
        //}
        static SyntaxNode RevomeDuplicaterBlank(SyntaxNode initialSource)
        {
            initialSource = new Rewriter().Visit(initialSource);
            return initialSource;
        }
        class Rewriter : CSharpSyntaxRewriter
        {
            bool _lastTokenIsAOpenBrace = false;
            object _endOfLineTrivia;

            private SyntaxKind _lastSpecialSyntax = SyntaxKind.None;
            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (_endOfLineTrivia == null)
                {
                    _endOfLineTrivia =
                        node
                            .SyntaxTree
                            .GetRoot()
                            .DescendantTrivia(descendIntoTrivia: true)
                            .FirstOrDefault(x => x.IsKind(SyntaxKind.EndOfLineTrivia));
                }
                if (node is UsingDirectiveSyntax)
                {
                    var output = CleanUpList(node.GetLeadingTrivia().ToList(), 0);
                    node = node.WithLeadingTrivia(output);

                    _lastSpecialSyntax = SyntaxKind.UsingKeyword;
                }
                else if (node is NamespaceDeclarationSyntax)
                {
                    node = CheckSyntaxNodeAfterUsingNode(node, SyntaxKind.NamespaceKeyword);
                }
                else if (node is ClassDeclarationSyntax)
                {
                    node = CheckSyntaxNodeAfterUsingNode(node, SyntaxKind.ClassKeyword);
                }
                //else if (node != null)
                //{
                //    if (node.GetLeadingTrivia().Count > 1)
                //    {
                //        var output = CleanUpList(node.GetLeadingTrivia().ToList());
                //        node = node.WithLeadingTrivia(output);
                //    }
                //}
                return base.Visit(node);
            }

            SyntaxNode CheckSyntaxNodeAfterUsingNode(SyntaxNode node, SyntaxKind syntaxNodeKind)
            {
                if (_lastSpecialSyntax == SyntaxKind.UsingKeyword)
                {
                    var output = CleanUpListUsings(node.GetLeadingTrivia().ToList());
                    node = node.WithLeadingTrivia(output);
                }
                _lastSpecialSyntax = syntaxNodeKind;
                //if (_myStack.Any())
                //{
                //    var topItem = _myStack.Pop();
                //    if (topItem == SyntaxKind.UsingKeyword)
                //    {
                //        var output = CleanUpListUsings(node.GetLeadingTrivia().ToList());
                //        node = node.WithLeadingTrivia(output);
                //        _myStack.Push(syntaxNodeKind);
                //    }
                //    else if (topItem != syntaxNodeKind) _myStack.Push(syntaxNodeKind);
                //}
                //else if (_myStack.Count == 0) _myStack.Push(syntaxNodeKind);

                return node;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (_lastTokenIsAOpenBrace)
                {
                    var oldList = token.LeadingTrivia.ToList();
                    var newList = CleanUpOpenBrace(oldList);
                    token = token.WithLeadingTrivia(newList);
                    _lastTokenIsAOpenBrace = false;
                }
                else if (token.IsKind(SyntaxKind.OpenBraceToken))
                {
                    _lastTokenIsAOpenBrace = true;
                }
                else if (token.IsKind(SyntaxKind.CloseBraceToken))
                {
                    var oldList = token.LeadingTrivia.ToList();
                    var newList = CleanUpCloseBrace(oldList);
                    token = token.WithLeadingTrivia(newList);
                }
                else if (token.LeadingTrivia.Count > 1)
                {
                    var output = CleanUpList(token.LeadingTrivia.ToList());
                    token = token.WithLeadingTrivia(output);
                }

                return base.VisitToken(token);
            }

            //public override SyntaxTriviaList VisitList(SyntaxTriviaList list)
            //{
            //    list = base.VisitList(list);
            //    return list;
            //    //if (list.Count == 1)
            //    //{
            //    //    _lastTokenIsAOpenBrace = list[0].Token.IsKind(SyntaxKind.OpenBraceToken);
            //    //    return list;
            //    //}

            //    //var newList = list.ToList();

            //    //var specialTriviasCount = FindSpecialTriviasCount(newList);

            //    //newList = CleanUpList(newList);

            //    //newList = ProcessSpecialTrivias(newList, specialTriviasCount, itsForCloseBrace: false);

            //    //list = SyntaxFactory.TriviaList(newList);
            //    //return list;
            //}

            //private List<SyntaxTrivia> CleanUpList(List<SyntaxTrivia> newList)
            //{
            //    var lineBreaksAtBeginning = newList.TakeWhile(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).Count();

            //    int numberOfBlankLine = 1;

            //    if (_lastTokenIsAOpenBrace)
            //    {
            //        _lastTokenIsAOpenBrace = false;
            //        newList = newList.Skip(lineBreaksAtBeginning).ToList();
            //    }
            //    //else if (mustCleanUpTopOfTrivialsList)
            //    //{
            //    //    if (lineBreaksAtBeginning > 1)
            //    //    {
            //    //        newList = newList.Skip(lineBreaksAtBeginning - 1).ToList();
            //    //    }
            //    //    else if (lineBreaksAtBeginning < 1)
            //    //    {
            //    //        newList.Insert(0, (SyntaxTrivia)_endOfLineTrivia);
            //    //    }
            //    //    mustCleanUpTopOfTrivialsList = false;
            //    //}
            //    return newList;
            //}
            List<SyntaxTrivia> CleanUpList(List<SyntaxTrivia> newList)
            {
                var lineBreaksAtBeginning = newList.TakeWhile(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).Count();

                if (lineBreaksAtBeginning > 1)
                {
                    newList = newList.Skip(lineBreaksAtBeginning - 1).ToList();
                }

                return newList;
            }
            List<SyntaxTrivia> CleanUpList(List<SyntaxTrivia> newList, int exactNumberOfBlanks)
            {
                var lineBreaksAtBeginning = newList.TakeWhile(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).Count();

                if (lineBreaksAtBeginning > exactNumberOfBlanks)
                {
                    newList = newList.Skip(lineBreaksAtBeginning - exactNumberOfBlanks).ToList();
                }
                else if (lineBreaksAtBeginning < exactNumberOfBlanks)
                {
                    for (int i = lineBreaksAtBeginning; i <= exactNumberOfBlanks; i++)
                    {
                        newList.Insert(0, (SyntaxTrivia)_endOfLineTrivia);
                    }
                }

                return newList;
            }
            List<SyntaxTrivia> CleanUpListUsings(List<SyntaxTrivia> newList)
            {
                return CleanUpList(newList, 1);
            }
            List<SyntaxTrivia> CleanUpOpenBrace(List<SyntaxTrivia> newList)
            {
                newList = CleanUpList(newList, 0);
                newList = ProcessSpecialTrivias(newList, FindSpecialTriviasCount(newList), itsForCloseBrace: false);
                return newList;
            }
            List<SyntaxTrivia> CleanUpCloseBrace(List<SyntaxTrivia> newList)
            {
                newList = CleanUpList(newList);
                newList = ProcessSpecialTrivias(newList, FindSpecialTriviasCount(newList), itsForCloseBrace: true);
                return newList;
            }
            int FindSpecialTriviasCount(IEnumerable<SyntaxTrivia> newList)
            {
                return
                    newList

                        .Count(t =>
                            t.IsKind(SyntaxKind.RegionDirectiveTrivia) ||
                            t.IsKind(SyntaxKind.EndRegionDirectiveTrivia) ||
                            t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                            t.IsKind(SyntaxKind.MultiLineCommentTrivia)
                        );
            }
            bool RemoveBlankDuplication(ref List<SyntaxTrivia> newList, SyntaxKind kind, int iterationIndex)
            {
                if (iterationIndex >= newList.Count) return false;

                var lineBreaksAtBeginning = newList.Skip(iterationIndex).TakeWhile(t => t.IsKind(kind)).Count();

                if (lineBreaksAtBeginning > 1)
                {
                    newList.RemoveRange(iterationIndex, lineBreaksAtBeginning - 1);
                }
                return lineBreaksAtBeginning > 0;
            }
            List<SyntaxTrivia> ProcessSpecialTrivias(IList<SyntaxTrivia> syntaxTrivias, int specialTriviasCount, bool itsForCloseBrace)
            {
                var newList = syntaxTrivias.ToList();

                var outputTriviasList = new List<SyntaxTrivia>();

                int specialTiviasCount = 0;

                for (int i = 0; i < newList.Count; i++)
                {
                    if (specialTiviasCount == specialTriviasCount)
                    {
                        if (itsForCloseBrace)
                        {
                            if (RemoveBlankDuplication(ref newList, SyntaxKind.EndOfLineTrivia, i))
                            {
                                i++;
                            }
                            if (RemoveBlankDuplication(ref newList, SyntaxKind.WhitespaceTrivia, i))
                            {
                                outputTriviasList.Add(newList[i]);
                            }
                            i = newList.Count;
                            continue;
                        }
                    }
                    if
                    (
                        (
                            newList[i].IsKind(SyntaxKind.EndOfLineTrivia) ||
                            newList[i].IsKind(SyntaxKind.WhitespaceTrivia) ||
                            newList[i].IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                            newList[i].IsKind(SyntaxKind.MultiLineCommentTrivia)
                        ) == false
                    )
                    {
                        outputTriviasList.Add(newList[i]);
                        specialTiviasCount++;
                        continue;
                    }

                    if (newList[i].IsKind(SyntaxKind.SingleLineCommentTrivia) || newList[i].IsKind(SyntaxKind.MultiLineCommentTrivia))
                    {
                        outputTriviasList.Add(newList[i]);
                        i++;
                        if (i < newList.Count && newList[i].IsKind(SyntaxKind.EndOfLineTrivia))
                        {
                            outputTriviasList.Add(newList[i]);
                        }
                        specialTiviasCount++;
                        continue;
                    }

                    if (RemoveBlankDuplication(ref newList, SyntaxKind.EndOfLineTrivia, i))
                    {
                        outputTriviasList.Add(newList[i]);
                        continue;
                    }
                    if (RemoveBlankDuplication(ref newList, SyntaxKind.WhitespaceTrivia, i))
                    {
                        outputTriviasList.Add(newList[i]);
                    }
                }
                return outputTriviasList;
            }
        }
    }
}
