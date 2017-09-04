using Geeks.GeeksProductivityTools.Definition;
using System;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class CodeCleanerFactory
    {
        public static ICodeCleaner Create(CodeCleanerType type)
        {
            switch (type)
            {
                case CodeCleanerType.NormalizeWhiteSpaces:
                    return new WhiteSpaceNormalizer();
                case CodeCleanerType.PrivateAccessModifier:
                    return new PrivateModifierRemover();
                case CodeCleanerType.OrganizeUsingDirectives:
                    return new UsingDirectiveOrganizer();
                default: return null; // TODO
            }
        }
    }
}
