﻿using Microsoft.VisualStudio.Shell;
using System;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup
{
    public abstract class BaseCodeCleanupCommand
    {
        public abstract void SetupCommands();

        protected abstract void CallBack(object sender, EventArgs e);

        protected void Item_BeforeQueryStatus(object sender, EventArgs e)
        {
            var cmd = sender as OleMenuCommand;
            var activeDoc = App.DTE.ActiveDocument;

            if (null != cmd && activeDoc != null)
            {
                var fileName = App.DTE.ActiveDocument.FullName.ToUpper();
                cmd.Visible = true;
            }
        }
    }
}
