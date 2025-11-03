/**
 This Code is published under the terms and conditions of the CC-BY-NC-ND-4.0
 (https://creativecommons.org/licenses/by-nc-nd/4.0)
 
 Please contribute to the current project.
 
 SPDX-License-Identifier: CC-BY-NC-ND-4.0
 @author: pdulvp@laposte.net
*/
using Pdulvp.EasyFirewall.Properties;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static System.Windows.Forms.ListViewItem;

namespace Pdulvp.EasyFirewall
{

    public partial class MainForm : Form
    {

        private int CurrentGroup = 1;

        private FwRules FwRules;

        public MainForm()
        {
            InitializeComponent();
            Localize();

            FwRules = new FwRules();

            // Obtain a handle to the system image list.
            WinIcons.SHFILEINFO shfi = new WinIcons.SHFILEINFO();
            IntPtr hSysImgList = WinIcons.SHGetFileInfo("",
                                                             0,
                                                             ref shfi,
                                                             (uint)Marshal.SizeOf(shfi),
                                                             WinIcons.SHGFI_SYSICONINDEX
                                                              | WinIcons.SHGFI_SMALLICON);
            Debug.Assert(hSysImgList != IntPtr.Zero);  // cross our fingers and hope to succeed!

            // Set the ListView control to use that image list.
            IntPtr hOldImgList = WinIcons.SendMessage(listView1.Handle,
                                                           WinIcons.LVM_SETIMAGELIST,
                                                           WinIcons.LVSIL_SMALL,
                                                           hSysImgList);

            // If the ListView control already had an image list, delete the old one.
            if (hOldImgList != IntPtr.Zero)
            {
                WinIcons.ImageList_Destroy(hOldImgList);
            }

            // Set up the ListView control's basic properties.
            // Put it in "Details" mode, create a column so that "Details" mode will work,
            // and set its theme so it will look like the one used by Explorer.
            listView1.View = View.Details;
            listView1.Columns.Add(Resources.name, 400);
            WinIcons.SetWindowTheme(listView1.Handle, "Explorer", null);

            listView1.Columns.Add(Resources.folder, 200);
            listView1.Columns.Add(Resources.productName, 100);
            listView1.Columns.Add(Resources.company, 100);

            FwRules.RulesLoadingStarted += (s, e) =>
            {
                listView1.Invoke(new MethodInvoker(delegate
                {
                    listView1.Items.Clear();
                    addToolStripMenuItem.Enabled = false;
                    refreshToolStripMenuItem.Enabled = false;
                }));
            };
            FwRules.RulesLoadingAdded += (s, e) =>
            {
                if (toolStripProgressBar1.Value != e && e % 10 == 0)
                {

                    listView1.Invoke(new MethodInvoker(delegate
                    {
                        toolStripProgressBar1.Value = e;
                    }));
                }
            };
            FwRules.RulesLoadingCompleted += (s, e) =>
            {
                listView1.Invoke(new MethodInvoker(delegate
                {
                    listView1.Items.AddRange(FwRules.Rules.Select(r => createItem(r)).ToArray());
                    GroupBy(CurrentGroup);
                    ResetStatusLine(null);
                    addToolStripMenuItem.Enabled = true;
                    refreshToolStripMenuItem.Enabled = true;
                    toolStripMenuItem2.Enabled = FwRules.Rules.Any(x => x.Inconsistent);
                }));
            };

            FwRules.load();
        }

        private void Localize()
        {
            toolStripMenuItem1.Text = Resources.github;
            advancedToolStripMenuItem.Text = Resources.advanced;
            refreshToolStripMenuItem.Text = Resources.refresh;
            refreshToolStripMenuItem.ToolTipText = Resources.refreshDesc;
            addToolStripMenuItem.Text = Resources.addApplication;
            fileToolStripMenuItem.Text = Resources.file;
            Text = Resources.title;
            deleteToolStripMenuItem.Text = Resources.delete;
            openFolderToolStripMenuItem.Text = Resources.openFolder;
            toolStripMenuItem2.Text = Resources.fixInconsistencies;
        }

        private void ResetStatusLine(Task obj)
        {
            toolStripProgressBar1.Value = 1;
        }

        private ListViewItem createItem(FwRule rule)
        {
            WinIcons.SHFILEINFO shfi = new WinIcons.SHFILEINFO();

            IntPtr himl = WinIcons.SHGetFileInfo(rule.ApplicationName,
                                                                      0,
                                                                      ref shfi,
                                                                      (uint)Marshal.SizeOf(shfi),
                                                                      WinIcons.SHGFI_DISPLAYNAME
                                                                        | WinIcons.SHGFI_SYSICONINDEX
                                                                        | WinIcons.SHGFI_SMALLICON);

            ListViewItem item = new ListViewItem(rule.ReadableName, shfi.iIcon);
            item.SubItems.Add(new ListViewSubItem(item, rule.ReadableProduct));
            item.SubItems.Add(new ListViewSubItem(item, rule.ProductName));
            item.SubItems.Add(new ListViewSubItem(item, rule.CompanyName));
            item.Tag = rule;

            if (rule.Deprecated)
            {
                item.ForeColor = Color.Orange;
                item.Font = new Font(listView1.Font, listView1.Font.Style | FontStyle.Strikeout);
            }

            return item;
        }


        private void GroupBy(int i)
        {
            listView1.Groups.Clear();
            foreach (ListViewItem item in listView1.Items)
            {
                if (i == 0)
                {
                    item.Group = getGroup(((FwRule)item.Tag).ApplicationName);
                }
                else if (i < item.SubItems.Count)
                {
                    item.Group = getGroup(item.SubItems[i].Text);
                }
                else
                {
                    item.Group = getGroup("");
                }
            }
        }
        private ListViewGroup getGroup(String name)
        {
            foreach (ListViewGroup grp in listView1.Groups)
            {
                if (name == grp.Header)
                {
                    return grp;
                }
            }
            ListViewGroup g = new ListViewGroup(name);
            listView1.Groups.Add(g);
            return g;
        }

        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
            else
                e.Effect = DragDropEffects.None;
        }

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            foreach (string ee in s)
            {
                if (!FwRules.Exists(ee))
                {
                    FwRule rule = FwRules.Create(ee);
                    listView1.Items.Add(createItem(rule));
                }
            }
            GroupBy(CurrentGroup);
        }


        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Multiselect = true;
            openFileDialog1.Filter = "(*.exe)|*.exe";
            openFileDialog1.FileOk += InsertFiles;
            openFileDialog1.ShowDialog();
        }

        public void InsertFiles(object sender, CancelEventArgs e)
        {
            string[] names = openFileDialog1.FileNames;
            foreach (string ee in names)
            {
                if (!FwRules.Exists(ee))
                {
                    FwRule rule = FwRules.Create(ee);
                    listView1.Items.Add(createItem(rule));
                }
            }
            openFileDialog1.FileOk -= InsertFiles;
            GroupBy(CurrentGroup);
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<FwRule> Rules = FwRules.Rules;

            foreach (ListViewItem i in listView1.SelectedItems)
            {
                FwRule rule = (FwRule)i.Tag;
                FwRules.Remove(rule);
                listView1.Items.Remove(i);
            }
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            CurrentGroup = e.Column;
            GroupBy(CurrentGroup);
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FwRules.load();
        }

        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem i in listView1.SelectedItems)
            {
                FwRule rule = (FwRule)i.Tag;
                Process.Start("explorer.exe", "/select, \"" + rule.ApplicationName + "\"");
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = "https://github.com/pdulvp/easy-firewall", UseShellExecute = true });
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            foreach (FwRule rule in FwRules.Rules.FindAll(x => x.Inconsistent))
            {
                FwRules.Fix(rule);
            }
            FwRules.load();
        }
    }

}
