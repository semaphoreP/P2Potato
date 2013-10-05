using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using P2Potato;
using Isis;

namespace P2PotatoGUI {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SplashForm s = new SplashForm();
            s.Show();
            Platform.startP2Potato();
            Console.WriteLine(Platform.isActive());
            s.Close();
            Application.Run(new MainForm());
        }
    }
}
