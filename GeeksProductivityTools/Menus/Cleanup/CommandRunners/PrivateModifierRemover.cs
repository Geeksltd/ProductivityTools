using EnvDTE;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public class PrivateModifierRemover : ICodeCleaner
    {
        public void Run(ProjectItem item) => RemoveExplicitPrivateModifiers(item);

        static void RemoveExplicitPrivateModifiers(ProjectItem item)
        {
            var actualSourceCode = item.ToSyntaxNode();
            var filePath = item.ToFullPathPropertyValue();

            var methodRemoverTask = Task.Run(() =>
            {
                var updatedSourceCode = new MethodTokenRemover().Remove(actualSourceCode, filePath);
                actualSourceCode = UpdateSourceCode(filePath, actualSourceCode, updatedSourceCode);
            });

            var fieldRemoverTask = methodRemoverTask.ContinueWith(antecedentTask =>
            {
                var updateSourceCode = new FieldTokenRemover().Remove(actualSourceCode, filePath);
                actualSourceCode = UpdateSourceCode(filePath, actualSourceCode, updateSourceCode);
            });

            var propertyRemoverTask = fieldRemoverTask.ContinueWith(antecedentTask =>
            {
                var updateSourceCode = new PropertyTokenRemover().Remove(actualSourceCode, filePath);
                actualSourceCode = UpdateSourceCode(filePath, actualSourceCode, updateSourceCode);
            });

            var nestedClassesRemoverTask = propertyRemoverTask.ContinueWith(antecedentTask =>
            {
                var updateSourceCode = new NestedClassTokenRemover().Remove(actualSourceCode, filePath);
                actualSourceCode = UpdateSourceCode(filePath, actualSourceCode, updateSourceCode);
            });

            Task.WaitAll(new[] { nestedClassesRemoverTask, propertyRemoverTask, fieldRemoverTask, methodRemoverTask });
        }

        static SyntaxNode UpdateSourceCode(string filePath, SyntaxNode actualSourceCode, SyntaxNode updatedSourceCode)
        {
            if (updatedSourceCode != null)
            {
                actualSourceCode = updatedSourceCode;
                actualSourceCode.WriteSourceTo(filePath);
            }

            return actualSourceCode;
        }
    }
}
