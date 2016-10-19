using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Internet_Connectivity_Keeper
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            string k_path = Path.GetDirectoryName(Application.ExecutablePath).ToString() + @"\log.txt";
            File.Delete(k_path);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InternetConnectivityKeeper());
        }
    }
}