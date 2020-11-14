/**
 This Code is published under the terms and conditions of the CC-BY-NC-ND-4.0
 (https://creativecommons.org/licenses/by-nc-nd/4.0)
 
 Please contribute to the current project.
 
 SPDX-License-Identifier: CC-BY-NC-ND-4.0
 @author: pdulvp@laposte.net
*/
using NetFwTypeLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using static System.Windows.Forms.ListViewItem;

namespace Pdulvp.EasyFirewall
{

    public partial class Form1 : Form
    {
        public static readonly String GROUP = "Easy Firewall";

        private int CurrentGroup = 1;

        private INetFwPolicy2 Policy;

        public Form1()
        {
            InitializeComponent();

            Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            Policy = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);

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
            listView1.Columns.Add("Name", 500);
            WinIcons.SetWindowTheme(listView1.Handle, "Explorer", null);

            listView1.Columns.Add("Folder", 100);
            listView1.Columns.Add("Product Name", 100);
            listView1.Columns.Add("Company", 100);

            loadRules();
            GroupBy(CurrentGroup);
        }

        private List<INetFwRule> getRules(INetFwPolicy2 policy)
        {
            return policy.Rules.Cast<INetFwRule>().ToList().FindAll(r => isMyRule(r));
        }

        private void loadRules()
        {

            // Obtain a handle to the system image list.
            listView1.Items.Clear();

            try
            {
                List<INetFwRule> myRules = getRules(Policy);

                toolStripProgressBar1.Maximum = myRules.Count;
                toolStripProgressBar1.Value = 0;

                List<INetFwRule> inversedRules = new List<INetFwRule>();
                foreach (INetFwRule rule in myRules)
                {
                    if (!inversedRules.Exists(obj => obj == rule))
                    {
                        INetFwRule inverse = getInverse(rule, myRules);
                        addItem(rule, inverse);
                        if (inverse != null)
                        {
                            inversedRules.Add(inverse);
                        }
                    }
                    toolStripProgressBar1.Value++;
                }
               
            }

            catch (Exception)
            {
                Console.WriteLine("Error deleting a Firewall rule");
            }
            System.Threading.Tasks.Task.Delay(2 * 1000).ContinueWith(ResetStatusLine);

        }

        private void ResetStatusLine(Task obj)
        {
            listView1.Invoke(new MethodInvoker(delegate
            {
                toolStripProgressBar1.Value = 1;
            }));

        }
        private void addItem(INetFwRule rule, INetFwRule inverse)
        {
            WinIcons.SHFILEINFO shfi = new WinIcons.SHFILEINFO();
            IntPtr hSysImgList = WinIcons.SHGetFileInfo("",
                                                             0,
                                                             ref shfi,
                                                             (uint)Marshal.SizeOf(shfi),
                                                             WinIcons.SHGFI_SYSICONINDEX
                                                              | WinIcons.SHGFI_SMALLICON);

            IntPtr himl = WinIcons.SHGetFileInfo(rule.ApplicationName,
                                                                      0,
                                                                      ref shfi,
                                                                      (uint)Marshal.SizeOf(shfi),
                                                                      WinIcons.SHGFI_DISPLAYNAME
                                                                        | WinIcons.SHGFI_SYSICONINDEX
                                                                        | WinIcons.SHGFI_SMALLICON);

            ListViewItem item = new ListViewItem(getRuleReadableName(rule), shfi.iIcon);

            item.SubItems.Add(new ListViewSubItem(item, getRuleReadableProduct(rule)));
            try
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(rule.ApplicationName);
                item.SubItems.Add(new ListViewSubItem(item, fvi.ProductName));
                item.SubItems.Add(new ListViewSubItem(item, fvi.CompanyName));
            }
            catch (Exception)
            {

            }
            item.Tag = rule;
            listView1.Items.Add(item);
        }

        private void GroupBy(int i)
        {
            listView1.Groups.Clear();
            foreach (ListViewItem item in listView1.Items)
            {
                if (i == 0)
                {
                    item.Group = getGroup(((INetFwRule)item.Tag).ApplicationName);
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

        private string getRuleReadableName(INetFwRule rule)
        {
            string direction = rule.Direction == NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN ? "(in)" : "(out)";
            return rule.ApplicationName;
        }

        private string getRuleReadableProduct(INetFwRule rule)
        {
            string name = rule.ApplicationName;
            name = name.Replace(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "");
            name = name.Replace(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "");
            name = name.Replace(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), "");
            name = name.Replace(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86), "");
            name = name.Replace(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), "");
            name = name.Replace(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "");
            name = name.Replace(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "");
            name = name.Replace(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "");
            name = name.Replace(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "");
            name = Regex.Replace(name, @"[A-Z]:\\(Games|Tools|Works)\\", @"\");
            return name.Split('\\')[1];
        }

        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
            else
                e.Effect = DragDropEffects.None;
        }

        private INetFwRule createRule(String path)
        {
            INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
            firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
            firewallRule.ApplicationName = path;
            firewallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
            firewallRule.Enabled = true;
            firewallRule.Profiles = (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL;
            firewallRule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY;
            firewallRule.EdgeTraversal = false;
            firewallRule.Name = getRuleReadableName(firewallRule);
            firewallRule.LocalAddresses = "*";
            firewallRule.RemoteAddresses = "*";
            firewallRule.Grouping = GROUP;
            return firewallRule;
        }

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            foreach (string ee in s)
            {
                INetFwRule firewallRule = createRule(ee);
                Policy.Rules.Add(firewallRule);
                INetFwRule firewallRule2 = createInverseRule(firewallRule);
                Policy.Rules.Add(firewallRule2);
                addItem(firewallRule, firewallRule2);
            }
            GroupBy(CurrentGroup);
        }

        private void listView1_ItemDrag(object sender, ItemDragEventArgs e)
        {

        }

        private INetFwRule getInverse(INetFwRule rule, List<INetFwRule> rules)
        {
            return rules.Find(r => r.Direction != rule.Direction && r.ApplicationName == rule.ApplicationName);
        }

        private bool isMyRule(INetFwRule rule)
        {
            if (rule.Grouping != null && (rule.Grouping.StartsWith(GROUP)))
            {
                return true;
            }
            return false;
        }

        private INetFwRule createInverseRule(INetFwRule rule)
        {
            INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
            firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
            firewallRule.ApplicationName = rule.ApplicationName;
            firewallRule.Direction = (rule.Direction == NetFwTypeLib.NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN ? NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT : NetFwTypeLib.NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN);
            firewallRule.Enabled = true;
            firewallRule.Profiles = rule.Profiles;
            firewallRule.Protocol = rule.Protocol;
            firewallRule.EdgeTraversal = rule.EdgeTraversal;
            firewallRule.Name = getRuleReadableName(rule);
            if (rule.LocalAddresses != null)
            {
                firewallRule.LocalAddresses = rule.LocalAddresses;
            }
            if (rule.RemoteAddresses != null)
            {
                firewallRule.RemoteAddresses = rule.RemoteAddresses;
            }
            if (rule.LocalPorts != null)
            {
                firewallRule.LocalPorts = rule.LocalPorts;
            }
            if (rule.RemotePorts != null)
            {
                firewallRule.RemotePorts = rule.RemotePorts;
            }
            if (rule.IcmpTypesAndCodes != null)
            {
                firewallRule.IcmpTypesAndCodes = rule.IcmpTypesAndCodes;
            }
            if (rule.InterfaceTypes != null)
            {
                firewallRule.InterfaceTypes = rule.InterfaceTypes;
            }
            if (rule.Interfaces != null)
            {
                firewallRule.Interfaces = rule.Interfaces;
            }

            firewallRule.Grouping = rule.Grouping;
            return firewallRule;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

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
                INetFwRule firewallRule = createRule(ee);
                Policy.Rules.Add(firewallRule);
                INetFwRule firewallRule2 = createInverseRule(firewallRule);
                Policy.Rules.Add(firewallRule2);
                addItem(firewallRule, firewallRule2);
            }
            openFileDialog1.FileOk -= InsertFiles;
            GroupBy(CurrentGroup);
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<INetFwRule> Rules = getRules(Policy);

            foreach (ListViewItem i in listView1.SelectedItems)
            {
                INetFwRule rule = (INetFwRule)i.Tag;
                try
                {
                    Policy.Rules.Remove(rule.Name);
                    INetFwRule inverse = getInverse(rule, Rules);
                    if (inverse != null)
                    {
                        Policy.Rules.Remove(inverse.Name);
                    }
                    listView1.Items.Remove(i);
                }
                catch (Exception eee)
                {
                    Console.WriteLine(eee);
                }
            }
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            CurrentGroup = e.Column;
            GroupBy(CurrentGroup);
        }

        private void repairToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                List<INetFwRule> CreatedRules = new List<INetFwRule>();
                List<INetFwRule> Rules = getRules(Policy);
                toolStripProgressBar1.Maximum = Rules.Count;
                toolStripProgressBar1.Value = 0;
                foreach (INetFwRule rule in Rules)
                {
                    string newName = getRuleReadableName(rule);
                    if (rule.Name != newName)
                    {
                        rule.Name = newName;
                    }
                    if (rule.Grouping != GROUP)
                    {
                        rule.Grouping = GROUP;
                    }
                    
                    INetFwRule inverse = getInverse(rule, Rules);
                    if (inverse == null)
                    {
                        INetFwRule firewallRule = createInverseRule(rule);
                        Policy.Rules.Add(firewallRule);
                    }
                    toolStripProgressBar1.Value++;
                }
            }

            catch (Exception r)
            {
                Console.WriteLine(r);
                Console.WriteLine("Error deleting a Firewall rule");
            }
            System.Threading.Tasks.Task.Delay(2 * 1000).ContinueWith(ResetStatusLine);
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadRules();
            GroupBy(CurrentGroup);
        }

        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem i in listView1.SelectedItems)
            {
                INetFwRule rule = (INetFwRule)i.Tag;
                Process.Start("explorer.exe", "/select, \"" + rule.ApplicationName + "\"");
            }
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void listView1_Resize(object sender, EventArgs e)
        {

        }

        private void cccToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/pdulvp/easy-firewall"); 
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/pdulvp/easy-firewall");
        }
    }

}
