using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;


namespace NativeWifi
{
    public class Debug
    {
        public static void log(string str)
        {
            string k_path = Path.GetDirectoryName(Application.ExecutablePath).ToString() + @"\log.txt";
            StreamWriter sr = new StreamWriter(k_path, append: true);
            sr.WriteLine(DateTime.Now.ToString() + ": " + str);
            sr.Close();
        }
    }
}

