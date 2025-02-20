using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pdulvp.EasyFirewall
{
    public class Culture : ConfigurationElement
    {
        [ConfigurationProperty("CultureInfo", IsRequired = false)]
        public string CultureInfoName
        {
            get
            {
                string res = (string) this["CultureInfo"];
                if (res == null || res.Length == 0)
                {
                    res = System.Globalization.CultureInfo.CurrentCulture.Name;
                }
                return res;
            }
            set
            {
                value = (string)this["CultureInfo"];
            }
        }

        public CultureInfo CultureInfo
        {
            get
            {
                return CultureInfo.GetCultureInfo(CultureInfoName);
            }
        }
    }
    public class ProductSettings : ConfigurationSection
    {
        [ConfigurationProperty("Culture")]
        public Culture Culture
        {
            get
            {
                return (Culture)this["Culture"];
            }
            set
            {
                value = (Culture)this["Culture"];
            }
        }
    }
}
