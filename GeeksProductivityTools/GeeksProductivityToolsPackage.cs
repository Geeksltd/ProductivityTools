using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using EnvDTE80;
using GeeksAddin;
using GeeksAddin.Attacher;
using GeeksAddin.FileFinder;
using GeeksAddin.FileToggle;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;

namespace Geeks.GeeksProductivityTools
{
    [ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F")]    // Microsoft.VisualStudio.VSConstants.UICONTEXT_NoSolution
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(OptionsPage), "Geeks productivity tools", "General", 0, 0, true)]
    [Guid(GuidList.GuidGeeksProductivityToolsPkgString)]
    public sealed class GeeksProductivityToolsPackage : Package
    {
        public GeeksProductivityToolsPackage() { }

        // Strongly reference events so that it's not GC'd
        EnvDTE.DocumentEvents docEvents;
        EnvDTE.SolutionEvents solEvents;
        EnvDTE.Events events;

        public static GeeksProductivityToolsPackage Instance { get; private set; }
        public VisualStudioWorkspace VsWorkspace { get; private set; }

        protected override void Initialize()
        {
            base.Initialize();
            App.Initialize(GetDialogPage(typeof(OptionsPage)) as OptionsPage);

            Instance = this;

            var componentModel = (IComponentModel)this.GetService(typeof(SComponentModel));
            VsWorkspace = componentModel.GetService<VisualStudioWorkspace>();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var menuCommandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (null != menuCommandService)
            {
                menuCommandService.AddCommand(new MenuCommand(CallAttacher,
                                               new CommandID(GuidList.GuidGeeksProductivityToolsCmdSet,
                                                            (int)PkgCmdIDList.CmdidAttacher)));

                menuCommandService.AddCommand(new MenuCommand(CallWebFileToggle,
                                               new CommandID(GuidList.GuidGeeksProductivityToolsCmdSet,
                                                            (int)PkgCmdIDList.CmdidWebFileToggle)));

                menuCommandService.AddCommand(new MenuCommand(CallFixtureFileToggle,
                                               new CommandID(GuidList.GuidGeeksProductivityToolsCmdSet,
                                                            (int)PkgCmdIDList.CmdidFixtureFileToggle)));

                menuCommandService.AddCommand(new MenuCommand(CallFileFinder,
                                               new CommandID(GuidList.GuidGeeksProductivityToolsCmdSet,
                                                            (int)PkgCmdIDList.CmdidFileFinder)));

                menuCommandService.AddCommand(new MenuCommand(CallMemberFinder,
                                               new CommandID(GuidList.GuidGeeksProductivityToolsCmdSet,
                                                            (int)PkgCmdIDList.CmdidMemberFinder)));

                menuCommandService.AddCommand(new MenuCommand(CallCssFinder,
                                               new CommandID(GuidList.GuidGeeksProductivityToolsCmdSet,
                                                            (int)PkgCmdIDList.CmdidCSSFinder)));

                menuCommandService.AddCommand(new MenuCommand(CallGotoNextFoundItem,
                                               new CommandID(GuidList.GuidGeeksProductivityToolsCmdSet,
                                                            (int)PkgCmdIDList.CmdidGotoNextFoundItem)));

                menuCommandService.AddCommand(new MenuCommand(CallGotoPreviousFoundItem,
                                               new CommandID(GuidList.GuidGeeksProductivityToolsCmdSet,
                                                            (int)PkgCmdIDList.CmdidGotoPreviousFoundItem)));

                // Set up menu items
                new Menus.OpenInMSharp.OpenInMSharpCodeWindow(menuCommandService).SetupCommands();
                new Menus.OpenInMSharp.OpenInMSharpSolutionExplorer(menuCommandService).SetupCommands();

                new Menus.Typescript(menuCommandService).SetupCommands();
                new Menus.RunBatchFiles(menuCommandService).SetupCommands();

                new Menus.Cleanup.PrivateModifier(menuCommandService).SetupCommands();
                new Menus.Cleanup.NormalizeWhiteSpaceCommand(menuCommandService).SetupCommands();
                new Menus.Cleanup.ConvertMembersToExpressionBodiedCommand(menuCommandService).SetupCommands();
                new Menus.Cleanup.ConvertFullNameTypesToBuiltInTypesCommand(menuCommandService).SetupCommands();
                new Menus.Cleanup.SimplyAsyncCallsCommand(menuCommandService).SetupCommands();
                new Menus.Cleanup.SortClassMembersCommand(menuCommandService).SetupCommands();
                new Menus.Cleanup.SimplifyClassFieldDeclarationsCommand(menuCommandService).SetupCommands();
                new Menus.Cleanup.OrganizeUsingDirectives(menuCommandService).SetupCommands();
                new Menus.Cleanup.ActionAllCodeCleanup(menuCommandService).SetupCommands();
            }

            SetCommandBindings();

            // Hook up event handlers
            events = App.DTE.Events;
            docEvents = events.DocumentEvents;
            solEvents = events.SolutionEvents;
            docEvents.DocumentSaved += DocumentEvents_DocumentSaved;
            solEvents.Opened += delegate { App.Initialize(GetDialogPage(typeof(OptionsPage)) as OptionsPage); };
        }

        void DocumentEvents_DocumentSaved(EnvDTE.Document document)
        {
            if (document.Name.EndsWith(".cs") ||
                document.Name.EndsWith(".css") ||
                document.Name.EndsWith(".js") ||
                document.Name.EndsWith(".ts"))
            {
                document.DTE.ExecuteCommand("Edit.FormatDocument");
            }

            if (!document.Saved) document.Save();

            Menus.Typescript.OnDocumentSaved(document);
        }

        void SetCommandBindings()
        {
            var commands = (Commands2)App.DTE.Commands;
            foreach (EnvDTE.Command cmd in commands)
            {
                if (cmd.Name == "File.CloseAllButThis")
                    cmd.Bindings = "Global::CTRL+SHIFT+F4";

                foreach (var gadget in All.Gadgets)
                {
                    if (gadget.CommandName == cmd.Name)
                    {
                        cmd.Bindings = gadget.Binding;
                        break;
                    }
                }
            }
        }

        void CallAttacher(object sender, EventArgs e) => new AttacherGadget().Run(App.DTE);

        void CallWebFileToggle(object sender, EventArgs e) => new FileToggleGadget().Run(App.DTE);

        void CallFixtureFileToggle(object sender, EventArgs e) => new FixtureFileToggleGadget().Run(App.DTE);

        void CallFileFinder(object sender, EventArgs e) => new FileFinderGadget().Run(App.DTE);

        void CallMemberFinder(object sender, EventArgs e) => new MemberFinderGadget().Run(App.DTE);

        void CallCssFinder(object sender, EventArgs e) => new StyleFinderGadget().Run(App.DTE);

        void CallGotoNextFoundItem(object sender, EventArgs e) => new GotoNextFoundItemGadget().Run(App.DTE);

        void CallGotoPreviousFoundItem(object sender, EventArgs e) => new GotoPreviousFoundItemGadget().Run(App.DTE);
    }
}
