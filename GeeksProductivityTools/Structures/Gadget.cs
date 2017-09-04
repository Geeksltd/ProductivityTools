﻿using EnvDTE80;

namespace GeeksAddin
{
    public abstract class Gadget
    {
        public string Title { get; protected set; }
        public string Name { get; protected set; }
        public string ShortKey { get; protected set; }
        public string ToolTip { get; protected set; }

        public string CommandName { get { return "Tools." + Name; } }
        public string Binding { get { return "Global::" + ShortKey; } }

        public abstract void Run(DTE2 app);
    }
}
