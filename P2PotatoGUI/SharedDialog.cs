using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using P2Potato;

namespace P2PotatoGUI {
    public partial class SharedDialog : Form {

        public delegate bool Handler(string username, string folder, string password);

        public Handler handler;
        public SharedDialog(Handler handler) {
            InitializeComponent();
            this.handler = handler;
        }

        private void loginButton_Click(object sender, EventArgs e) {
            string username = usernameBox.Text;
            string folder = folderBox.Text;
            string password = passwordBox.Text;
            if (handler(username, folder, password)) {
                Close();
            } else {
                MessageBox.Show("Invalid username, folder, password combination.");
            }
        }

        private void closeButton_Click(object sender, EventArgs e) {
            Close();
        }
    }
}
