using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace P2PotatoGUI {
    public partial class DoubleDialog : Form {
        public delegate bool DialogHandler(string text, string text1);
        DialogHandler handler;

        public DoubleDialog(string question, string question1, DialogHandler handler, bool hideChars = false, bool hideChars1 = false) {
            InitializeComponent();
            label.Text = question;
            label1.Text = question1;
            this.handler = handler;
            this.textBox.UseSystemPasswordChar = hideChars;
            this.textBox1.UseSystemPasswordChar = hideChars1;
        }

        private void okButton_Click(object sender, EventArgs e) {
            if (handler(textBox.Text, textBox1.Text)) {
                Close();
            }
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            Close();
        }
    }
}
