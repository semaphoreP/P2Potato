using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using P2Potato;

namespace P2PotatoGUI {
    public partial class SharedFoldersForm : Form {

        public delegate void SuccessHandler();

        SuccessHandler handler;

        public SharedFoldersForm(SuccessHandler handler) {
            InitializeComponent();
            this.handler = handler;
            this.Load += SharedFoldersForm_Load;
        }

        private void SharedFoldersForm_Load(object sender, EventArgs e) {
            setupListView();
            refreshListView();
        }

        public void setupListView() {
            ImageList largeImageList = new ImageList();
            largeImageList.Images.Add(Bitmap.FromFile("images" + System.IO.Path.DirectorySeparatorChar + "folder.png"));
            largeImageList.ImageSize = new Size(64, 64);
            largeImageList.ColorDepth = ColorDepth.Depth24Bit;
            ImageList smallImageList = new ImageList();
            smallImageList.ImageSize = new Size(32, 32);
            smallImageList.ColorDepth = ColorDepth.Depth24Bit;
            smallImageList.Images.Add(Bitmap.FromFile("images" + System.IO.Path.DirectorySeparatorChar + "folder-small.png"));
            listView.LargeImageList = largeImageList;
            listView.SmallImageList = smallImageList;
            listView.View = View.List;
            EventHandler itemActivated = delegate(object sender, EventArgs e) {
                if (listView.SelectedItems.Count > 0) {
                    string filename = listView.SelectedItems[0].Text;
                    string[] arr = Regex.Split(filename, "://");
                    new SingleDialog("Password for shared folder:", delegate(string password) {
                        if (Platform.cdShared(arr[0], arr[1], password)) {
                            handler();
                            Close();
                            return true;
                        } else {
                            return false;
                        }
                    }, true).ShowDialog();
                } else {
                    refreshListView();
                }
            };
            listView.ItemActivate += itemActivated;
        }

        public void refreshListView() {
            string lsString = Platform.lsShared();
            listView.BeginUpdate();
            listView.Items.Clear();
            ListViewItem item;
            if (lsString != null) {
                string[] list = lsString.Split('\n');
                for (int i = 1; i < list.Length - 1; i++) {
                    string str = list[i];
                    item = new ListViewItem(str, 0);
                    listView.Items.Add(item);
                }
            }
            listView.EndUpdate();
        }
    }
}
