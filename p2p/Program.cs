using System;
using System.Text;
using System.IO;
using System.Windows.Forms;
using Isis;

namespace P2Potato {
    class Program {
        private static string PROGRAM_TITLE = "P2Potato";

        private void logout() {
            Platform.logout();
            Console.Clear();
            Console.WriteLine("Type 'login' to receive a login prompt");
            while (runLoginConsole());
        }

        /// <summary>
        /// Loops until a user enters either a valid username and password or
        /// creates a new user.
        /// </summary>
        private void login() {
            string username = null;
            string password = null;
            // Begin User login
            while (!Platform.isLoggedIn()) {
                username = null;
                password = null;
                while (username == null) {
                    username = promptUser("Please enter your username: ");
                }
                int tries = 0;
                do {
                    while (password == null && tries < 4) {
                        password = returnPassword();
                        tries++;
                        if (password.Length < 6) {
                            password = null;
                        }
                    }
                } while (tries < 4 && Platform.login(username, password));
            }
        }

        /// <summary>
        /// Sets up all starting state for Isis.
        /// </summary>
        public void startP2Potato() {
            //Environment.SetEnvironmentVariable("ISIS_HOSTS", "127.0.0.1");
            //Environment.SetEnvironmentVariable("ISIS_TCP_ONLY", "true");
            //Environment.SetEnvironmentVariable("ISIS_UNICAST_ONLY", "true");
            Console.Title = PROGRAM_TITLE + " - Your P2P Dropbox Alternative";
            Platform.startP2Potato();
            Console.WriteLine("P2Potato has started. Type 'login' to receive a login prompt");
            while (runLoginConsole());
            Console.WriteLine("Welcome to " + PROGRAM_TITLE + ", " + Platform.getUsername());
        }

        /// <summary>
        /// Cleans up isis to shutdown.
        /// </summary>
        public void stopP2Potato() {
            Platform.stopP2Potato();
        }

        /// <summary>
        /// Starts a password prompt that hides the typed characters
        /// Returns the password string that was inputed
        /// </summary>
        /// <returns>The user inputted password</returns>
        public static string returnPassword() {
            Console.Write("Enter password (must be at least 6 char): ");
            string password = "";
            ConsoleKeyInfo info = Console.ReadKey(true);
            while (info.Key != ConsoleKey.Enter) {
                if (info.Key != ConsoleKey.Backspace) {
                    password += info.KeyChar;
                    info = Console.ReadKey(true);
                } else if (info.Key == ConsoleKey.Backspace) {
                    if (!String.IsNullOrEmpty(password)) {
                        password = password.Substring(0, password.Length - 1);
                    }
                    info = Console.ReadKey(true);
                }
            }
            Console.WriteLine();
            return password;
        }

        /// <summary>
        /// Prompts the user for a open "request" and returns a filename string.
        /// read is true is uploading, false if downloading (let's you choose a filepath that doesn't exist yet)
        /// Returns null if no file is given.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="read"></param>
        /// <param name="console"></param>
        /// <returns></returns>
        private string openFilePromptUser(string request, bool read, bool console = true) {
            if (console) {
                Console.Write(request);
                string s = Console.ReadLine();
                return File.Exists(s) || !read ? s : null;
            } else if (read) {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                dialog.Filter = "All files (*.*)|*.*";
                dialog.FilterIndex = 1;
                dialog.RestoreDirectory = true;
                dialog.Title = "Choose a File To Open";
                return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
            } else {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                dialog.Filter = "All Files (*.*)|*.*";
                dialog.FilterIndex = 1;
                dialog.RestoreDirectory = true;
                dialog.Title = "Choose a File To Save";
                return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
            }
        }

        /// <summary>
        /// Prompts user for a request and returns a string
        /// Returns null if nothing was inputed
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private string promptUser(string request) {
            Console.Write(request);
            string s = Console.ReadLine();
            if (s.IndexOf('\\') >= 0 || s.IndexOf('*') >= 0 || s.IndexOf(':') >= 0) {
                Console.WriteLine("'\\:*' are invalid characters");
                return null;
            }
            return s.Length > 0 ? s : null;
        }

        /// <summary>
        /// 
        /// </summary>
        private void downloadFile() {
            string filename = promptUser("Name of file in P2PFS: ");
            string filepath = openFilePromptUser("Path to file: ", false, false);
            if (Platform.downloadFile(filename, filepath)) {
                Console.WriteLine("File download successful.");
            } else {
                Console.WriteLine("File download unsuccessful.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void deleteFile() {
            Platform.deleteFile(promptUser("Name of file in P2PFS: "));
        }

        /// <summary>
        /// 
        /// </summary>
        private void uploadFile() {
            string filepath = openFilePromptUser("Path to file: ", true, false);
            string filename = promptUser("Name of file in P2PFS: ");
            if (Platform.uploadFile(filepath, filename)) {
                Console.WriteLine("File upload successful.");
            } else {
                Console.WriteLine("File upload unsuccessful.");
            }
        }

        private void showHelp(string x) {
            string ans;
            switch(x) {
                case null:
                    ans = "help\npwd\nls\ncd\nmkdir\nrmdir\nupload\ndownload\ndelete\nclear\nlogout\nexit\nmkShared\ncdShared\nexitShared\nlsShared\nwhoami\n\nType help cmd for more help";
                    break;
                case "help":
                    ans = "help\n\tUsage: help - Gives list of commands\n\tUsage: help cmd - Gives help on command 'cmd'";
                    break;
                case "pwd":
                    ans = "pwd\n\tUsage: pwd - Prints current working directory";
                    break;
                case "ls":
                    ans = "ls\n\tUsage: ls - Lists contents of current directory\n\tUsage: ls path - Lists contents of directory given by 'path'";
                    break;
                case "cd":
                    ans = "cd\n\tUsage: cd - Changes current directory to the directory given. Absolute and relative paths are accepted.";
                    break;
                case "mkdir":
                    ans = "mkdir\n\tUsage: mkdir newdirpath - Makes a directory at 'newdirpath'. Absolute and relative paths are accepted.";
                    break;
                case "rmdir":
                    ans = "rmdir\n\tUsage: rmdir dirpath - Removes a directoy at 'dirpath'. Absolute and relative paths are accepted. Can only be deleted if directory is empty.";
                    break;
                case "upload":
                    ans = "upload\n\tUsage: upload - Brings up a prompt to upload a file from P2Potato.";
                    break;
                case "delete":
                    ans = "delete\n\tUsage: delete - Deletes the specified file from P2Potato.";
                    break;
                case "clear":
                    ans = "clear\n\tUsage: clear - Clears the screen.";
                    break;
                case "logout":
                    ans = "logout\n\tUsage: logout - Logs out the user from P2Potato.";
                    break;
                case "exit":
                    ans = "exit\n\tUsage: exit - Exits P2Potato.";
                    break;
                default:
                    ans = "help - Unrecognized command";
                    break;
            }
            Console.WriteLine(ans);
        }

        /// <summary>
        /// Creates one line of a "pseudo" console
        /// </summary>
        public bool runConsole() {
            Console.Write(Platform.getUsername() + "@" + Platform.pwd() + "$ ");
            string input = Console.ReadLine();
            string[] values = input.Split(' ');
            switch (values[0]) {
                case "help":
                    showHelp(values.Length < 2 ? null : values[1]);
                    break;
                case "pwd":
                    Console.WriteLine(Platform.pwd());
                    break;
                case "ls":
                    if (values.Length == 1) {
                        Console.WriteLine(Platform.ls());
                    } else {
                        Console.WriteLine(Platform.ls(values[1]));
                    }
                    break;
                case "cd":
                    if (values.Length < 2) {
                        Console.WriteLine("Not enough arguments");
                    } else {
                        if (!Platform.cd(values[1])) {
                            Console.WriteLine("Could not move to specified directory");
                        }
                    }
                    break;
                case "mkdir":
                    if (values.Length < 2) {
                        Console.WriteLine("Not enough arguments");
                    } else {
                        if (!Platform.mkdir(values[1])) {
                            Console.WriteLine("Unable to make directory");
                        }
                    }
                    break;
                case "rmdir":
                    if (values.Length < 2) {
                        Console.WriteLine("Not enough arguments");
                    } else {
                        if (!Platform.rmdir(values[1])) {
                            Console.WriteLine("Unable to remove directory");
                        }
                    }
                    break;
                case "mkShared":
                    string fname = promptUser("Name of shared folder: ");
                    string pass = returnPassword();
                    Platform.makeSharedFolder(fname, pass);
                    break;
                case "cdShared":
                    string user = promptUser("Owner of shared folder: ");
                    string destname = promptUser("Name of shared folder: ");
                    string sharedpass = returnPassword();
                    Platform.cdShared(user, destname, sharedpass);
                    break;
                case "exitShared":
                    Platform.exitShared();
                    break;
                case "lsShared":
                    Console.WriteLine(Platform.lsShared());
                    break;
                case "upload":
                    uploadFile();
                    break;
                case "download":
                    downloadFile();
                    break;
                case "delete":
                    deleteFile();
                    break;
                case "clear":
                    Console.Clear();
                    break;
                case "exit":
                    Console.WriteLine("Shutting down...");
                    return false;
                case "logout":
                    logout();
                    break;
                case "whoami":
                    Console.WriteLine(Platform.getUsername());
                    break;
                default:
                    Console.WriteLine("Unrecognized command\nType 'help' for help");
                    break;
            }
            return true;
        }

        /// <summary>
        /// Creates one line of a "pseudo" console
        /// </summary>
        public bool runLoginConsole() {
            Console.Write("$ ");
            string input = Console.ReadLine();
            string[] values = input.Split(' ');
            switch (values[0]) {
                case "login":
                    login();
                    return false;
                case "help":
                    Console.WriteLine("help\nlogin\nclear\nexit");
                    break;
                case "clear":
                    Console.Clear();
                    break;
                case "exit":
                    Console.WriteLine("Shutting down...");
                    stopP2Potato();
                    Environment.Exit(0);
                    return false;
                default:
                    Console.WriteLine("Unrecognized command\nType 'help' for help");
                    break;
            }
            return true;
        }

        public void runP2Potato() {
            while (runConsole());
        }

        [STAThread]
        static void Main(string[] args) {
            Program p = new Program();
            p.startP2Potato();
            p.runP2Potato();
            p.stopP2Potato();
        }
    }
}
