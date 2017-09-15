using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public static class SyntaxTokenExtensions
    {
        public static SyntaxNode RemovePrivateTokens(this SyntaxNode root, List<SyntaxToken> tokens, string filePath)
        {
            if (!tokens.Any()) return root;

            return root.ReplaceTokens(tokens, MakeReplacementToken(tokens));
        }

        static Func<SyntaxToken, SyntaxToken, SyntaxToken> MakeReplacementToken(List<SyntaxToken> tokens)
        {
            // replace with the LeadingTrivia so that the comments (if any) will not be lost also the private keyword is replaced at the same time
            return (oldToken, newToken) => SyntaxFactory.ParseToken(oldToken.LeadingTrivia.ToFullString());
        }

        static object _lockFileWrite = new object();
        public static void WriteSourceTo(this SyntaxNode sourceCode, string filePath)
        {
            lock (_lockFileWrite)
            {
                System.Text.Encoding encoding;
                using (var reader = new StreamReader(filePath))
                {
                    encoding = reader.CurrentEncoding;
                }
                using (var write = new StreamWriter(filePath, false, encoding))
                    write.Write(sourceCode.ToFullString());
            }
        }

        public static SyntaxNode ToSyntaxNode(this ProjectItem item)
        {
            return CSharpSyntaxTree.ParseText(File.ReadAllText(item.ToFullPathPropertyValue())).GetRoot();
        }

        public static string ToFullPathPropertyValue(this ProjectItem item)
        {
            return item.Properties.Item("FullPath").Value.ToString();
        }
    }
}
