using NetFwTypeLib;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.ListViewItem;

namespace Pdulvp.EasyFirewall
{
    internal class FwUtil
    {
        public static string getRuleReadableName(INetFwRule rule)
        {
            string direction = rule.Direction == NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN ? "(in)" : "(out)";
            return rule.ApplicationName;
        }
    }
    internal class FwRule
    {
        internal INetFwRule rule;

        internal INetFwRule inverse;

        internal bool loadedInfo = false;

        internal string productName = null;

        internal string companyName = null;

        public string ApplicationName { get { return rule.ApplicationName; } }

        public string ReadableName { get { return FwUtil.getRuleReadableName(rule); } }
        public string ReadableProduct { get { return getRuleReadableProduct(rule); } }

        private void loadInfo()
        {
            if (!loadedInfo)
            {
                try
                {
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(rule.ApplicationName);
                    productName = fvi.ProductName == null ? "" : fvi.ProductName;
                    companyName = fvi.CompanyName == null ? "" : fvi.CompanyName;
                }
                catch (Exception)
                {
                    productName = "";
                    companyName = "";
                }
                loadedInfo = true;
            }
        }

        public string ProductName { get {
            if (productName == null)
            {
                loadInfo();
            }
            return productName;
        } }

        public string CompanyName
        {
            get
            {
                if (companyName == null)
                {
                    loadInfo();
                }
                return companyName;
            }
        }

        public bool Deprecated { get { return isDeprecated(rule); } }

        private bool isDeprecated(INetFwRule rule)
        {
            return !File.Exists(rule.ApplicationName);
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
    }

    internal class FwRules
    {
        public static readonly String GROUP = "Easy Firewall";
        private INetFwPolicy2 Policy;

        public FwRules()
        {
            Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            Policy = (INetFwPolicy2) Activator.CreateInstance(tNetFwPolicy2);
        }

        private List<FwRule> rules;

        public List<FwRule> Rules { get { return rules; } }

        public event System.EventHandler RulesLoadingStarted;

        public event System.EventHandler RulesLoadingCompleted;

        public event System.EventHandler<FwRule> RulesLoadingAdded;

        protected virtual void OnRulesLoadingStarted()
        {
            if (RulesLoadingStarted != null) RulesLoadingStarted(this, EventArgs.Empty);
        }
        protected virtual void OnRulesLoadingCompleted()
        {
            if (RulesLoadingCompleted != null) RulesLoadingCompleted(this, EventArgs.Empty);
        }

        protected virtual void OnRulesLoadingAdded(FwRule rule)
        {
            if (RulesLoadingAdded != null) RulesLoadingAdded(this, rule);
        }




        public void load()
        {
            System.Threading.Tasks.Task.Delay(100).ContinueWith(loadRules);
        }

        private List<INetFwRule> getRules()
        {
            return Policy.Rules.Cast<INetFwRule>().ToList().FindAll(r => isMyRule(r));
        }

        private bool isMyRule(INetFwRule rule)
        {
            if (rule.Grouping != null && (rule.Grouping.StartsWith(GROUP)))
            {
                return true;
            }
            return false;
        }
        private void loadRules(Task task)
        {
            OnRulesLoadingStarted();
            rules = new List<FwRule>();

            // Obtain a handle to the system image list.

            try
                {
                    List<INetFwRule> myRules = getRules();

                    List<INetFwRule> inversedRules = new List<INetFwRule>();
                    foreach (INetFwRule rule in myRules)
                    {
                        if (!inversedRules.Exists(obj => obj == rule))
                        {
                            INetFwRule inverse = getInverse(rule, myRules);
                            FwRule newRule = Create(rule, inverse);
                            Rules.Add(newRule);
                            if (inverse != null)
                            {
                                inversedRules.Add(inverse);
                            }
                            OnRulesLoadingAdded(newRule);
                        }
                    }
                }

                catch (Exception e)
                {
                    Console.WriteLine("Error deleting a Firewall rule");
                }



            OnRulesLoadingCompleted();
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
            firewallRule.Name = FwUtil.getRuleReadableName(firewallRule);
            firewallRule.LocalAddresses = "*";
            firewallRule.RemoteAddresses = "*";
            firewallRule.Grouping = GROUP;
            return firewallRule;
        }

        private INetFwRule getInverse(INetFwRule rule, List<INetFwRule> rules)
        {
            return rules.Find(r => r.Direction != rule.Direction && r.ApplicationName == rule.ApplicationName);
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
            firewallRule.Name = FwUtil.getRuleReadableName(rule);
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

        internal void Remove(FwRule rule)
        {
            try
            {
                Policy.Rules.Remove(rule.rule.Name);
                if (rule.inverse != null)
                {
                    Policy.Rules.Remove(rule.inverse.Name);
                }
                Rules.Remove(rule);
            }
            catch (Exception eee)
            {
                Console.WriteLine(eee);
            }
        }

        internal bool Exists(string filepath)
        {
            return Rules.Exists(r => r.rule.ApplicationName == filepath);
        }

        internal FwRule Create(string filepath)
        {
            INetFwRule firewallRule = createRule(filepath);
            Policy.Rules.Add(firewallRule);
            INetFwRule firewallRule2 = createInverseRule(firewallRule);
            Policy.Rules.Add(firewallRule2);
            return Create(firewallRule, firewallRule2);
        }

        internal FwRule Create(INetFwRule nrule, INetFwRule inverse)
        {
            FwRule rule = new FwRule();;
            rule.rule = nrule;
            rule.inverse = inverse;
            return rule;
        }
    }
}
