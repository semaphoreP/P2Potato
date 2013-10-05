using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using P2Potato;

namespace P2PotatoGUI {
    public partial class MainForm : Form {

        private const string TITLE_TEXT = "P2Potato - Your P2P Dropbox Alternative";

        private bool isLoggedIn = false;
        private bool sharedMode = false;

        public MainForm() {
            InitializeComponent();
            //this.ShowInTaskbar = false;
            MenuItem[] items = new MenuItem[2];
            items[0] = new MenuItem("Open", delegate(object sender, EventArgs e) {
                this.WindowState = FormWindowState.Normal;
            });
            items[1] = new MenuItem("Exit", exitToolStripMenuItem_Click);
            this.FormClosing += this.mainForm_FormClosing;
            notifyIcon.ContextMenu = new ContextMenu(items);
        }

        private void MainForm_Load(object sender, EventArgs e) {
            setupListView();
        }

        public void setupListView() {
            ImageList largeImageList = new ImageList();
            largeImageList.Images.Add(Bitmap.FromFile("images" + System.IO.Path.DirectorySeparatorChar + "folder.png"));
            largeImageList.Images.Add(Bitmap.FromFile("images" + System.IO.Path.DirectorySeparatorChar + "file.png"));
            largeImageList.Images.Add(Bitmap.FromFile("images" + System.IO.Path.DirectorySeparatorChar + "folder-up.png"));
            largeImageList.ImageSize = new Size(64, 64);
            largeImageList.ColorDepth = ColorDepth.Depth24Bit;
            ImageList smallImageList = new ImageList();
            smallImageList.Images.Add(Bitmap.FromFile("images" + System.IO.Path.DirectorySeparatorChar + "folder-small.png"));
            smallImageList.Images.Add(Bitmap.FromFile("images" + System.IO.Path.DirectorySeparatorChar + "file-small.png"));
            largeImageList.Images.Add(Bitmap.FromFile("images" + System.IO.Path.DirectorySeparatorChar + "folder-up-small.png"));
            smallImageList.ColorDepth = ColorDepth.Depth24Bit;
            smallImageList.ImageSize = new Size(32, 32);
            listView.LargeImageList = largeImageList;
            listView.SmallImageList = smallImageList;
            //listView.View = View.Tile;
            listView.View = View.LargeIcon;
            listView.Groups.Add(new ListViewGroup("Folders"));
            listView.Groups.Add(new ListViewGroup("Files"));
            listView.ItemSelectionChanged += delegate(object sender, ListViewItemSelectionChangedEventArgs e) {
                if (listView.SelectedItems.Count > 0) {
                    switch (listView.SelectedItems[0].Group.Header) {
                        case "Folders":
                            if (listView.SelectedItems[0].Text != "..") {
                                downloadToolStripMenuItem.Text = "Open Folder";
                                deleteToolStripMenuItem.Text = "Delete Folder";
                                commandSeparator.Visible = true;
                                downloadToolStripMenuItem.Visible = true;
                                deleteToolStripMenuItem.Visible = true;
                            } else {
                                downloadToolStripMenuItem.Text = "Go Back";
                                commandSeparator.Visible = true;
                                downloadToolStripMenuItem.Visible = true;
                                deleteToolStripMenuItem.Visible = false;
                            }
                            break;
                        case "Files":
                            downloadToolStripMenuItem.Text = "Download File";
                            deleteToolStripMenuItem.Text = "Delete File";
                            commandSeparator.Visible = true;
                            downloadToolStripMenuItem.Visible = true;
                            deleteToolStripMenuItem.Visible = true;
                            break;
                        default:
                            throw new InvalidOperationException("Not in a group");
                    }
                } else {
                    commandSeparator.Visible = false;
                    downloadToolStripMenuItem.Visible = false;
                    deleteToolStripMenuItem.Visible = false;
                }
            };
            EventHandler itemActivated = delegate(object sender, EventArgs e) {
                if (listView.SelectedItems.Count > 0) {
                    string filename = listView.SelectedItems[0].Text;
                    switch (listView.SelectedItems[0].Group.Header) {
                        case "Folders":
                            Platform.cd(filename);
                            refreshListView();
                            break;
                        case "Files":
                            SaveFileDialog dialog = new SaveFileDialog();
                            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                            dialog.Filter = "All Files (*.*)|*.*";
                            dialog.FilterIndex = 1;
                            dialog.RestoreDirectory = true;
                            dialog.Title = "Choose a File To Save";
                            if (dialog.ShowDialog() == DialogResult.OK) {
                                if (Platform.downloadFile(filename, dialog.FileName)) {
                                    MessageBox.Show(filename + " has been saved to " + dialog.FileName);
                                } else {
                                    MessageBox.Show("Download unsuccessful");
                                }
                            }
                            break;
                        default:
                            throw new InvalidOperationException("Not in a group");
                    }
                }
                refreshListView();
            };
            listView.ItemActivate += itemActivated;
            downloadToolStripMenuItem.Click += itemActivated;
        }

        private int countSlashes(string text) {
            int count = 0;
            for (int i = 0; i < text.Length; i++) {
                if (text[i] == '/') {
                    count++;
                }
            }
            return count;
        }

        public void refreshListView() {
            string lsString = Platform.ls();
            pwdLabel.Text = Platform.pwd();
            listView.BeginUpdate();
            listView.Items.Clear();
            ListViewItem item;
            if (pwdLabel.Text != "/" && !(pwdLabel.Text.Contains("://") && countSlashes(pwdLabel.Text) == 3)) {
                item = new ListViewItem("..", 2, listView.Groups[0]); // Add one for ".."
                emptyLabel.Visible = false;
                listView.Items.Add(item);
            } else if (lsString == null || lsString.Split('\n').Length == 2) {
                emptyLabel.Visible = true;
            } else {
                emptyLabel.Visible = false;
            }
            if (lsString != null) {
                string[] list = lsString.Split('\n');
                for (int i = 1; i < list.Length - 1; i++) {
                    string str = list[i];
                    int typeIndex = str[str.Length - 1] == '/' ? 0 : 1;
                    item = new ListViewItem(str, typeIndex, listView.Groups[typeIndex]);
                    listView.Items.Add(item);
                }
            }
            listView.EndUpdate();
            commandSeparator.Visible = false;
            downloadToolStripMenuItem.Visible = false;
            deleteToolStripMenuItem.Visible = false;
        }

        public void login() {
            isLoggedIn = true;
            commandsToolStripMenuItem.Visible = true;
            sharingToolStripMenuItem.Visible = true;
            loginToolStripMenuItem.Text = "Logout";
            this.Text = TITLE_TEXT + " - " + Platform.getUsername();
            pwdLabel.Visible = true;
            listView.Visible = true;
            refreshListView();
        }

        public void logout() {
            isLoggedIn = false;
            commandsToolStripMenuItem.Visible = false;
            sharingToolStripMenuItem.Visible = false;
            pwdLabel.Visible = false;
            listView.Visible = false;
            emptyLabel.Visible = false;
            loginToolStripMenuItem.Text = "Login";
            this.Text = TITLE_TEXT;
            Platform.logout();
        }

        private void mainForm_FormClosing(object sender, FormClosingEventArgs e) {
            switch (e.CloseReason) {
                case CloseReason.UserClosing:
                    // Cancel close and minimize to notification tray
                    e.Cancel = true;
                    this.WindowState = FormWindowState.Minimized;
                    this.ShowInTaskbar = false;
                    break;
            }
        }

        private void loginToolStripMenuItem_Click(object sender, EventArgs e) {
            if (!isLoggedIn) {
                new LoginForm("Log in to P2Potato", delegate(string username, string password) {
                    if (Platform.login(username, password)) {
                        login();
                        return true;
                    }
                    return false;
                }).ShowDialog();
            } else {
                logout();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Platform.stopP2Potato();
            Application.Exit();
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e) {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }

        private void newFolderToolStripMenuItem_Click(object sender, EventArgs e) {
            new SingleDialog("Folder name:", delegate(string text) {
                bool valid = Platform.mkdir(text);
                if (!valid) {
                    MessageBox.Show("Invalid operation");
                } else {
                    refreshListView();
                }
                return valid;
            }).ShowDialog();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e) {
            if (listView.SelectedItems.Count > 0) {
                string filename = listView.SelectedItems[0].Text;
                switch (listView.SelectedItems[0].Group.Header) {
                    case "Folders":
                        if (Platform.rmdir(filename)) {
                            MessageBox.Show("Folder deleted.");
                        } else {
                            MessageBox.Show("Folder must be empty to be deleted.");
                        }
                        refreshListView();
                        break;
                    case "Files":
                        if (Platform.deleteFile(filename)) {
                            MessageBox.Show("File deleted.");
                            refreshListView();
                        }
                        break;
                    default:
                        throw new InvalidOperationException("Not in a group");
                }
            }
        }

        private void uploadToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            dialog.Filter = "All files (*.*)|*.*";
            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;
            dialog.Title = "Choose a File To Open";
            if (dialog.ShowDialog() == DialogResult.OK) {
                new SingleDialog("Name / Path of file in P2P", delegate(string filename) {
                    if (!Platform.uploadFile(dialog.FileName, filename)) {
                        MessageBox.Show("Upload unsuccessful");
                    }
                    refreshListView();
                    return true;
                }).ShowDialog();
            }
        }

        private void openSharedToolStripMenuItem_Click(object sender, EventArgs e) {
           if (!sharedMode) {
                new SharedDialog(delegate(string username, string foldername, string password) {
                        if (Platform.cdShared(username, foldername, password)) {
                            openSharedToolStripMenuItem.Text = "Exit Shared Folder";
                            sharedMode = true;
                            refreshListView();
                            return true;
                        }
                        return false;
                }).ShowDialog();
            } else {
                Platform.exitShared();
                sharedMode = false;
                openSharedToolStripMenuItem.Text = "Open Shared Folder";
                refreshListView();
            }
        }

        private void newSharedFolderToolStripMenuItem_Click(object sender, EventArgs e) {
            new DoubleDialog("Shared Folder name", "Password", delegate(string foldername, string password) {
                if (Platform.makeSharedFolder(foldername, password)) {
                    refreshListView();
                    MessageBox.Show("Shared folder, " + foldername + ", has been created.");
                    return true;
                }
                return false;
            }, false, true).ShowDialog();
        }

        private void viewKnownSharedFoldersToolStripMenuItem_Click(object sender, EventArgs e) {
            new SharedFoldersForm(delegate() {
                openSharedToolStripMenuItem.Text = "Exit Shared Folder";
                sharedMode = true;
                refreshListView();
            }).ShowDialog();
        }

        private void emptyLabel_Click(object sender, EventArgs e) {
            refreshListView();
        }
    }
}
