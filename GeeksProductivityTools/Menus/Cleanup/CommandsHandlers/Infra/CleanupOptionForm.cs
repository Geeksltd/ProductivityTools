using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Geeks.GeeksProductivityTools.Definition;

namespace Geeks.GeeksProductivityTools.Menus.Cleanup.CommandsHandlers.Infra
{
    public partial class CleanupOptionForm : Form
    {
        static CleanupOptionForm()
        {
            Instance = new CleanupOptionForm();
        }
        public static CleanupOptionForm Instance { get; set; }
        public CodeCleanerType[] SelectedTypes { get; private set; }


        CleanupOptionForm()
        {
            InitializeComponent();
            base.ShowInTaskbar = false;
            base.WindowState = FormWindowState.Normal;
            this.FormClosed += CleanupOptionForm_FormClosed;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(0xF0, 0xF0, 0xF0);
            bInsideClose = false;
            CreateControls();
        }

        private void CreateControls()
        {
            foreach (var itemValue in Enum.GetValues(typeof(CodeCleanerType)))
            {
                NewCheckbox((CodeCleanerType)itemValue);
            }
        }

        private void NewCheckbox(CodeCleanerType itemValue)
        {
            if (itemValue == CodeCleanerType.All) return;
            if (itemValue == CodeCleanerType.Unspecified) return;

            checkedListBox1.Items.Add(new CheckBoxItem { Name = itemValue.ToString(), CleanerType = itemValue });
        }


        public class CheckBoxItem
        {
            public string Name { get; set; }
            public CodeCleanerType CleanerType { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            SelectedTypes =
                checkedListBox1.CheckedItems?.Cast<CheckBoxItem>().Select(x => x.CleanerType).ToArray();

            bInsideClose = true;
            this.Close();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {

            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, false);
            }

        }

        bool bInsideClose = false;
        private void CleanupOptionForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!bInsideClose)
            {
                SelectedTypes = null;
            }
        }
    }
}
