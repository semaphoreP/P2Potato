using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using P2Potato;

namespace P2PotatoGUI {
    public partial class LoginForm : Form {

        public delegate bool LoginHandler(string username, string password);

        public LoginHandler loginHandler;
        public LoginForm(string title, LoginHandler handler) {
            InitializeComponent();
            this.titleLabel.Text = title;
            this.loginHandler = handler;
        }

        private void loginButton_Click(object sender, EventArgs e) {
            string username = usernameBox.Text;
            string password = passwordBox.Text;
            if (loginHandler(username, password)) {
                Close();
            } else {
                MessageBox.Show("Invalid username and password combination");
            }
        }

        private void closeButton_Click(object sender, EventArgs e) {
            Close();
        }
    }
}
