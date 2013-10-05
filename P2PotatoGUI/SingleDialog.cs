using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace P2PotatoGUI {
    public partial class SingleDialog : Form {
        public delegate bool DialogHandler(string text);
        DialogHandler handler;

        public SingleDialog(string question, DialogHandler handler, bool hideChars = false) {
            InitializeComponent();
            label.Text = question;
            this.handler = handler;
            this.textBox.UseSystemPasswordChar = hideChars;
        }

        private void okButton_Click(object sender, EventArgs e) {
            if (handler(textBox.Text)) {
                Close();
            }
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            Close();
        }
    }
}
