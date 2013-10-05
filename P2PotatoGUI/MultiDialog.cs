using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace P2PotatoGUI {
    public partial class MultiDialog : Form {
        public delegate bool OKHandler(string[] vals);
        public delegate void SuccessHandler();
        public delegate void ErrorHandler();
        OKHandler okHandler;
        ErrorHandler errorHandler;
        SuccessHandler successHandler;


        public MultiDialog(string[] questions, bool[] hideInput, OKHandler okHandler, SuccessHandler successHandler = null, ErrorHandler errorHandler = null) {
            InitializeComponent();
            questionPanel.SuspendLayout();
            for (int i = 0; i < questions.Length; i++) {
                Panel panel = new Panel();
                Label label = new Label();
                label.Text = questions[i];
                TextBox textBox = new TextBox();
                label.Dock = DockStyle.Left;
                textBox.Dock = DockStyle.Right;
                textBox.UseSystemPasswordChar = hideInput == null ? false : hideInput[i];
                panel.Controls.Add(label);
                panel.Controls.Add(textBox);
                this.questionPanel.Controls.Add(panel);
            }
            questionPanel.ResumeLayout();
            this.okHandler = okHandler;
            this.errorHandler = errorHandler;
            this.successHandler = successHandler;
        }

        private void okButton_Click(object sender, EventArgs e) {
            string[] vals = new string[questionPanel.Controls.Count];
            for (int i = 0; i < questionPanel.Controls.Count; i++) {
                vals[i] = questionPanel.Controls[i].Controls[1].Text;
            }
            if (okHandler(vals)) {
                Close();
                if (successHandler != null) {
                    successHandler();
                }
            } else {
                if (errorHandler != null) {
                    errorHandler();
                }
            }
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            Close();
        }
    }
}
