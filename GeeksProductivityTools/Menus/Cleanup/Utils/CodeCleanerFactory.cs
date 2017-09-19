using Geeks.GeeksProductivityTools.Definition;

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
                case CodeCleanerType.ConvertMembersToExpressionBodied:
                    return new ConvertMembersToExpressionBodied();
                case CodeCleanerType.FullNameTypesToBuiltInTypes:
                    return new ConvertFullNameTypesToBuiltInTypes();
                case CodeCleanerType.SortClassMembersCommand:
                    return new SortClassMembers();
                case CodeCleanerType.SimplyAsyncCallsCommand:
                    return new SimplyAsyncCalls();
                case CodeCleanerType.SimplifyClassFieldDeclarationsCommand:
                    return new SimplifyClassFieldDeclarations();
                case CodeCleanerType.PrivateAccessModifier:
                    return new PrivateModifierRemover();
                case CodeCleanerType.OrganizeUsingDirectives:
                    return new UsingDirectiveOrganizer();
                default: return null; // TODO
            }
        }
    }
}
