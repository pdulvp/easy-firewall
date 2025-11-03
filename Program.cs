/**
 This Code is published under the terms and conditions of the CC-BY-NC-ND-4.0
 (https://creativecommons.org/licenses/by-nc-nd/4.0)
 
 Please contribute to the current project.
 
 SPDX-License-Identifier: CC-BY-NC-ND-4.0
 @author: pdulvp@laposte.net
*/
using Pdulvp.EasyFirewall.Properties;
using System.Globalization;

namespace Pdulvp.EasyFirewall
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string? customSetting = AppContext.GetData("Culture.CultureInfo") as string;
            if (customSetting == null)
            {
                Console.WriteLine("CultureInfo is not defined");
            }
            else
            {
                Resources.Culture = CultureInfo.GetCultureInfo(customSetting);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }
}