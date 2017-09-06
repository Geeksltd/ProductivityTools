﻿namespace GeeksAddin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EnvDTE;
    using EnvDTE80;
    using Geeks.GeeksProductivityTools;

    static class DteExtensions
    {
        /// <summary>
        /// Returns the selected text or null if selected text cannot be found
        /// </summary>
        public static string GetSelectedText(this DTE2 app)
        {
            if (app.ActiveDocument != null)
            {
                dynamic selection = app.ActiveDocument.Selection;
                if (selection != null)
                {
                    var selectedText = selection.Text as string;
                    if (selectedText.HasValue())
                        return selectedText;
                }
            }

            return null;
        }

        public static string GetCurrentProjectPath(this DTE2 app)
        {
            if (app.ActiveDocument == null)
                return null;

            var documentPath = app.ActiveDocument.Path.ToLower();
            var allProjectPaths = Utils.FindSolutionDirectories(app);

            if (allProjectPaths == null) return documentPath;

            return allProjectPaths.FirstOrDefault(p => documentPath.StartsWith(p.ToLower()));
        }

        ///<summary>Gets the full paths to the currently selected item(s) in the Solution Explorer.</summary>
        public static IEnumerable<string> GetSelectedItemPaths()
        {
            var items = (Array)App.DTE.ToolWindows.SolutionExplorer.SelectedItems;
            foreach (UIHierarchyItem selItem in items)
            {
                var item = selItem.Object as ProjectItem;

                if (item != null)
                    yield return item.Properties.Item("FullPath").Value.ToString();
            }
        }

        ///<summary>Gets the full paths to the currently selected item(s) in the Solution Explorer.</summary>
        public static IEnumerable<string> GetRelativeItemPaths()
        {
            return GetSelectedItemPaths().Select(fullPath => fullPath.Remove(System.IO.Path.GetDirectoryName(App.DTE.Solution.FullName)));
        }

        public const string SolutionItemsFolder = "Solution Items";

        ///<summary>Gets the Solution Items solution folder in the current solution, creating it if it doesn't exist.</summary>
        public static Project GetSolutionItemsProject()
        {
            var solution = App.DTE.Solution as Solution2;
            return solution.Projects
                           .OfType<Project>()
                           .FirstOrDefault(p => p.Name.Equals(SolutionItemsFolder, StringComparison.OrdinalIgnoreCase))
                      ?? solution.AddSolutionFolder(SolutionItemsFolder);
        }
    }
}
