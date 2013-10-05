using System;
using System.Text;
using System.IO;
using Isis;
using System.Security.Cryptography;

namespace P2Potato {
    public static class Platform {
        public const uint MSG_SIZE = 32 * 1024 * 128; // Max message size is 32 * 1024 * 256
        public const uint SPUD_SIZE = 32 * 1024 * 112 - Version.DATA_OFFSET;
        private const string SPUD_DIRECTORY = "_p2potato";
        private const string SPUD_SUFFIX = "\n";
        private const string USER_TABLE = "3_83170597020751";
        private const string MASHED = "24_2338852577505";
        private const string SMASHED = "0_8935769662791675";

        private static Version version;

        private static Group potatoes;
        private static string username = null;
        private static string globalUsername = null;
        private static string password = null;
        private static string globalPassword = null;
        private static string curDirectory = "/";
        private static string curLocalDirectory = "/";
        private static bool shareMode = false;

        public static bool isLoggedIn() {
            return globalUsername != null;
        }
        public static string getUsername() {
            return globalUsername;
        }

        public static bool login(string newUsername, string newPassword) {
            if (username != null || newUsername == null || newPassword == null || newPassword.Length < 6 || !Platform.registerName(newUsername, newPassword)) {
                return false;
            } else {
                username = newUsername;
                globalUsername = username;
                password = newPassword;
                globalPassword = password;
                return true;
            }
        }

        public static void logout() {
            shareMode = false;
            username = null;
            password = null;
            globalUsername = null;
            globalPassword = null;
            curDirectory = "/";
            curLocalDirectory = "/";
        }

        public static string pwd() {
            return shareMode ? username + curDirectory : curDirectory;
        }

        private static string toSpudKey(string s, uint i) {
            string title = s + SPUD_SUFFIX + username + "-#" + i;
            return Util.fixBase64(Hashing.ComputeHash(title, Encoding.UTF8.GetBytes(password)));
        }

        /// <summary>
        /// Encrypts the key through a hash with a salt.
        /// </summary>
        /// <param name="s"></param>
        /// <returns>The hashed string</returns>
        private static string makeEncryptedKey(string s) {
            string title = s + SPUD_SUFFIX + username + "%key%";
            return Util.fixBase64(Hashing.ComputeHash(title, Encoding.UTF8.GetBytes(password)));
        }

        /// <summary>
        /// Returns the key for user table lookup
        /// </summary>
        /// <param name="s"></param>
        /// <returns>The hashed string</returns>
        private static string getUserTableKey() {
            return Util.fixBase64(Hashing.ComputeHash(USER_TABLE, Encoding.UTF8.GetBytes(MASHED)));
        }

        private static string getLsKey(string user, string directory) {
            if (shareMode) {
                return getSharedKey(user, directory, password);
            }
            return Util.fixBase64(Hashing.ComputeHash("~" + user + ":" + directory, Encoding.UTF8.GetBytes(password)));
        }

        /// <summary>
        /// Returns the place to put a file
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string getDHTFilepath(object key) {
            return SPUD_DIRECTORY + System.IO.Path.DirectorySeparatorChar + key.ToString();
        }

        /// <summary>
        /// Changes the current directory
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static bool cd(string dir) {
            dir = PathBase.addTrailingSlash(dir);
            string newDir = getAbsolutePath(curDirectory, dir);
            if (!version.DHTExists(getLsKey(username, newDir))) {
                return false;
            }
            curDirectory = newDir;
            return true;
        }

        public static string getAbsolutePath(string curdir, string dir) {
            if (shareMode) {
                return SharedPath.getAbsolutePath(curdir, dir);
            } else {
                return SingleUserPath.getAbsolutePath(curdir, dir);
            }
        }

        /// <summary>
        /// Creates a new directory.
        /// </summary>
        /// <param name="newDir">
        /// A relative or absolute path to the new folder with or without a trailing slash.
        /// Does not create arbitrary tree structures to directory.
        /// </param>
        /// <returns>True if the directory was created. False otherwise</returns>
        public static bool mkdir(string newDir) {
            if (newDir == "/") {
                return false;
            }
            newDir = SingleUserPath.addTrailingSlash(newDir);
            string newPath = getAbsolutePath(curDirectory, newDir);
            if (newPath == null) {
                return false;
            }
            version.startCheckpoint();
            if (version.DHTExists(getLsKey(username, newPath))) {
                version.endCheckpoint();
                return false;
            }
            string edir = EncDec.Encrypt("\n", password);
            version.DHTPut(getLsKey(username, newPath), edir);
            updateLs(newPath.Substring(newPath.LastIndexOf(PathBase.PATH_SEPARATOR, newPath.Length - 2) + 1), true, newPath.Substring(0, newPath.LastIndexOf(PathBase.PATH_SEPARATOR, newPath.Length - 2) + 1));
            version.endCheckpoint();
            return true;
        }

        /// <summary>
        /// Removes a directory. Will not delete if directory is not empty
        /// </summary>
        /// <param name="newDir">
        /// A relative or absolute path to the folder with or without a trailing slash.
        /// </param>
        /// <returns>True if the directory no longer exists. False otherwise</returns>
        public static bool rmdir(string newDir) {
            if (newDir == "/") {
                return false;
            }
            newDir = SingleUserPath.addTrailingSlash(newDir);
            string newPath = getAbsolutePath(curDirectory, newDir);
            if (newPath == null) {
                return false;
            }
            version.startCheckpoint();
            string edirlist = version.DHTGetAsString(getLsKey(username, newPath));
            if (edirlist == null) {
                version.endCheckpoint();
                return true;
            }
            string dirlist = EncDec.Decrypt(edirlist, password);
            if (dirlist != "\n") {
                Debug.WriteLine("Directory is not empty");
                version.endCheckpoint();
                return false;
            }
            version.DHTRemove(getLsKey(username, newPath));
            updateLs(newPath.Substring(newPath.LastIndexOf(PathBase.PATH_SEPARATOR, newPath.Length - 2) + 1), false, newPath.Substring(0, newPath.LastIndexOf(PathBase.PATH_SEPARATOR, newPath.Length - 2) + 1));
            version.endCheckpoint();
            return true;
        }

        /// <summary>
        /// Request a list of everything currently on the server.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static string ls(string dir = null) {
            if (dir == null) {
                dir = curDirectory;
            } else {
                dir = getAbsolutePath(curDirectory, PathBase.addTrailingSlash(dir));
            }
            string encrypted = version.DHTGetAsString(getLsKey(username, dir));
            if (encrypted == null) {
                return null;
            }
            string list = EncDec.Decrypt(encrypted, password);
            return list;
        }

        /// <summary>
        /// This function must be called underneath a Version "checkpoint"
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="add"></param>
        /// <param name="dir"></param>
        private static void updateLs(string filename, bool add, string dir = null) {
            if (dir == null) {
                dir = curDirectory;
            }
            string elist = version.DHTGetAsString(getLsKey(username, dir));
            // ls file doesn't exist yet.
            if (elist == null) {
                if (add) {
                    string newList = "\n" + filename + "\n";
                    string eNewList = EncDec.Encrypt(newList, password);
                    version.DHTPut(getLsKey(username, dir), eNewList);
                }
                return;
            }
            string list = EncDec.Decrypt(elist, password);
            int index = list.IndexOf("\n" + filename + "\n");
            if (add && index < 0) {
                list += filename + "\n";
                elist = EncDec.Encrypt(list, password);
                version.DHTPut(getLsKey(username, dir), elist);
                return;
            }
            if (!add && index >= 0) {
                list = list.Remove(index + 1, filename.Length + 1);
                elist = EncDec.Encrypt(list, password);
                version.DHTPut(getLsKey(username, dir), elist);
                return;
            }
        }

        private static bool checkUserTable(string name, string hashed) {
            string usertbl = version.DHTGetAsString(getUserTableKey());

            // No such file exists, first person to log in
            if (usertbl == null) {
                string users = "\r\n" + name + "\t" + EncDec.Encrypt(hashed, hashed) + "\r";
                version.DHTPut(getUserTableKey(), users);
                return true;
            }

            //Check if user already is registered
            if (usertbl.IndexOf("\n" + name + "\t") >= 0) {
                if (usertbl.IndexOf("\n" + name + "\t" + EncDec.Encrypt(hashed, hashed) + "\r") >= 0) {
                    return true;
                }
                else {
                    Debug.WriteLine("P2Potato.Platform.registerName: Incorrect password");
                    return false;
                }
            }
            //Register the user and logs in
            usertbl += "\n" + name + "\t" + EncDec.Encrypt(hashed, hashed) + "\r";
            version.DHTPut(getUserTableKey(), usertbl);
            return true;
        }

        private static string getUserKey(string name) {
            return Hashing.ComputeHash(name, Encoding.Default.GetBytes(SMASHED));
        }

        private static bool checkNameOfPotato(string name, string hashed) {
            string ehashed = version.DHTGetAsString(getUserKey(name));
            // No such file exists, new accout
            if (ehashed == null) {
                ehashed = EncDec.Encrypt(hashed, hashed);
                version.DHTPut(getUserKey(name), ehashed);
                return true;
            }
            if (ehashed == EncDec.Encrypt(hashed, hashed)) {
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Login with name-password pair. If name has not been used before,
        /// sets password and logs in.  Otherwise checks for incorrect password.
        /// </summary>
        /// <param name="name">username to register</param>
        /// <param name="password">password</param>
        /// <returns>True if successful</returns>
        private static bool registerName(string name, string password) {
            if (name == null || password == null) {
                return false;
            }
            //only storing hashed password
            string hashed = Hashing.ComputeHash(password, Encoding.UTF8.GetBytes(name));
            version.startCheckpoint();
            bool success = checkNameOfPotato(name, hashed);
            //bool success = checkUserTable(name, hashed);
            version.endCheckpoint();
            return success;
        }

        /// <summary>
        /// Writes a file if it is more "recent" in our scheme. More recent means
        /// that the timestamp is later, and in the case of ties, the address
        /// which is greater in value wins.
        /// </summary>
        /// <param name="filename">The name of the file to write</param>
        /// <param name="data">The bytes which compose the file</param>
        private static bool writeLatestFile(string filename, byte[] versionedData) {
            return version.writeLatestFile(versionedData, getDHTFilepath(filename));
        }

        public static bool isActive() {
            return IsisSystem.IsisActive;
        }

        public static void stopP2Potato() {
            if (IsisSystem.IsisActive) {
                IsisSystem.Shutdown();
            }
        }

        /// <summary>
        /// Starts up the P2Potato system.
        /// </summary>
        public static void startP2Potato() {
            if (!Directory.Exists(SPUD_DIRECTORY)) {
                DirectoryInfo di = Directory.CreateDirectory(SPUD_DIRECTORY);
                di.Attributes = FileAttributes.Directory | FileAttributes.Hidden | FileAttributes.NotContentIndexed;
            }
            Debug.WriteLine("P2Potato.Platform.startP2Potato: Starting up Isis2...");
            IsisSystem.Start();
            Debug.WriteLine("P2Potato.Platform.startP2Potato: Creating group...");
            potatoes = new Group("p2potatoes");
            Debug.WriteLine("P2Potato.Platform.startP2Potato: Enabling DHT...");
            Cache<object, object> cache = new Cache<object, object>(125, delegate(object key) {
                byte[] bytes = new byte[MSG_SIZE];
                string filepath = getDHTFilepath(key);
                if (!File.Exists(filepath)) {
                    return null;
                }
                using (FileStream filestream = File.OpenRead(getDHTFilepath(key))) {
                    int len = filestream.Read(bytes, 0, bytes.Length);
                    byte[] smallerBytes = new byte[len];
                    Buffer.BlockCopy(bytes, 0, smallerBytes, 0, len);
                    return Msg.BArrayToObjects(smallerBytes)[0];
                }
            }, delegate(object key, object value) {
                try {
                    byte[] bytes = (byte[])value;
                    return writeLatestFile((string)key, bytes);
                } catch (Exception e) {
                    Debug.WriteLine(e);
                    return false;
                }
            }, delegate(object key) {
                return writeLatestFile((string)key, new byte[0]);
            });
            Group.DHTPutHandlerDelegate putHandler = delegate(object key, object value) {
                byte[] data = Version.removeVersionData((byte[])value);
                if (data != null && data.Length == 0) {
                    return cache.remove(key);
                } else {
                    return cache.put(key, value);
                }
            };
            Group.DHTGetHandlerDelegate getHandler = delegate(object key) {
                return cache.get(key);
            };
            ChkptMaker chkptMakeHandler = delegate(View v) {
                try {
                    DirectoryInfo di = Directory.CreateDirectory(SPUD_DIRECTORY);
                    byte[] bytes = new byte[MSG_SIZE];
                    byte[] smallerBytes;
                    int len;
                    foreach (FileInfo fi in di.GetFiles()) {
                        using (FileStream fs = fi.OpenRead()) {
                            len = fs.Read(bytes, 0, bytes.Length);
                            smallerBytes = new byte[len];
                            Buffer.BlockCopy(bytes, 0, smallerBytes, 0, len);
                            potatoes.SendChkpt(Msg.toBArray(fi.Name), smallerBytes);
                        }
                    }
                } catch (Exception e) {
                    Debug.WriteLine(e);
                }
            };
            DHTChkptLoader chkptLoadHandler = delegate(byte[] filenameBytes, byte[] data) {
                try {
                    object[] objects = Msg.BArrayToObjects(filenameBytes);
                    if (objects.Length == 0) {
                        return;
                    }
                    string filename = (string)objects[0];
                    writeLatestFile(filename, data);
                } catch (Exception e) {
                    Debug.WriteLine(e);
                }
            };
            potatoes.DHTSetHandlers(putHandler, getHandler, chkptMakeHandler, chkptLoadHandler);
            potatoes.DHTEnable(1, 1, 1);
            Debug.WriteLine("Joining group...");
            potatoes.Join();
            version = new Version(potatoes, potatoes.myPhysIPAddr);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static bool uploadFile(string filepath, string filename) {
            filename = getAbsolutePath(curDirectory, filename);
            if (filename == null) {
                return false;
            }
            try {
                version.startCheckpoint();
                using (FileStream filestream = File.OpenRead(filepath)) {
                    byte[] espud;
                    int len;
                    uint spudNum = 0;
                    byte[] spud = new byte[SPUD_SIZE];
                    // Read for more data if there is more data to read
                    while ((len = filestream.Read(spud, 0, (int)SPUD_SIZE)) > 0) {
                        string spudKey = toSpudKey(filename, spudNum);
                        if (len < SPUD_SIZE) {
                            byte[] smaller = new byte[len];
                            Buffer.BlockCopy(spud, 0, smaller, 0, len);
                            espud = EncDec.Encrypt(smaller, password);
                        } else {
                            espud = EncDec.Encrypt(spud, password);
                        }
                        version.DHTPut(spudKey, espud);
                        Debug.WriteLine("Spud #" + spudNum + " of length " + espud.Length + " written: " + spudKey);
                        spudNum++;
                    }

                    // Check if there is any cleanup to do
                    object obj = version.DHTGet(makeEncryptedKey(filename));
                    if (obj != null) {
                        uint oldSpudNum = (uint)obj;
                        while (oldSpudNum > spudNum) {
                            // Delete the old parts
                            version.DHTRemove(toSpudKey(filename, oldSpudNum - 1));
                            Debug.WriteLine("Spud number: " + (oldSpudNum - 1) + " deleted");
                            oldSpudNum--;
                        }
                    }
                    version.DHTPut(makeEncryptedKey(filename), (UInt32)spudNum);
                    updateLs(filename.Substring(filename.LastIndexOf(PathBase.PATH_SEPARATOR) + 1), true, filename.Substring(0, filename.LastIndexOf(PathBase.PATH_SEPARATOR) + 1));
                }
                version.endCheckpoint();
                return true;
            } catch (Exception e) {
                Debug.WriteLine(e);
                version.endCheckpoint();
                return false;
            }
        }

        enum DownloadError {
            InvalidTime,
            Failure,
            Success
        };

        private static DownloadError downloadFileHelper(string filename, string filepath) {
            Tuple<object, Tuple<long, int>> obj = version.DHTGetWithVersionData(makeEncryptedKey(filename));

            // No such file exists
            if (obj == null || obj.Item1 == null) {
                Debug.WriteLine("No such key exists.");
                return DownloadError.Failure;
            }
            uint numSpuds = (UInt32)obj.Item1;
            long timestamp = obj.Item2.Item1;
            int address = obj.Item2.Item2;
            Debug.WriteLine("Getting " + numSpuds + " spuds...");

            // NOTE: Make this asynchronous in the future?
            try {
                using (FileStream filestream = File.OpenWrite(filepath)) {
                    byte[] espud;
                    byte[] spud;
                    for (uint i = 0; i < numSpuds; i++) {
                        Debug.WriteLine("Getting file: " + toSpudKey(filename, i));
                        Tuple<object, Tuple<long, int>> tuple = version.DHTGetWithVersionData(toSpudKey(filename, i));
                        if (tuple == null) {
                            Debug.WriteLine("Spud #" + i + " is invalid.");
                            return DownloadError.Failure;
                        }
                        if (timestamp != tuple.Item2.Item1 || address != tuple.Item2.Item2) {
                            return DownloadError.InvalidTime;
                        }

                        espud = (byte[])tuple.Item1;
                        //espud = version.DHTGetAsBytes(toSpudKey(filename, i));
                        if (espud == null) {
                            Debug.WriteLine("Spud #" + i + " is invalid.");
                            return DownloadError.Failure;
                        }
                        Debug.WriteLine("Encrypted file has size " + espud.Length);
                        if (espud.Length == 0) {
                            Debug.WriteLine("File is incomplete");
                            return DownloadError.Failure;
                        }
                        spud = EncDec.Decrypt(espud, password);
                        filestream.Write(spud, 0, spud.Length);
                        Debug.WriteLine("Spud #" + i + " written to disk. Length: " + spud.Length);
                    }
                }
                return DownloadError.Success;
            } catch (Exception e) {
                Debug.WriteLine(e);
                return DownloadError.Failure;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static bool downloadFile(string filename, string filepath) {
            filename = getAbsolutePath(curDirectory, filename);
            if (filename == null) {
                return false;
            }
            // Timeout after 10 times
            for (uint i = 0; i < 10; i++) {
                DownloadError e = downloadFileHelper(filename, filepath);
                switch (e) {
                    case DownloadError.Success:
                        return true;
                    case DownloadError.Failure:
                        return false;
                    case DownloadError.InvalidTime:
                        break;
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static bool deleteFile(string filename) {
            filename = getAbsolutePath(curDirectory, filename);
            if (filename == null) {
                return false;
            }
            version.startCheckpoint();
            object obj = version.DHTGet(makeEncryptedKey(filename));
            if (obj == null) {
                Debug.WriteLine("No such file exists.");
                return false;
            }

            uint numSpuds = (uint)obj;
            for (uint i = 0; i < numSpuds; i++) {
                string spudKey = toSpudKey(filename, i);
                Debug.WriteLine("Deleting spud: " + spudKey);
                version.DHTRemove(spudKey);
            }
            string encryptedKey = makeEncryptedKey(filename);
            Debug.WriteLine("Deleting key: " + encryptedKey);
            version.DHTRemove(encryptedKey);
            updateLs(filename.Substring(filename.LastIndexOf(PathBase.PATH_SEPARATOR) + 1), false, filename.Substring(0, filename.LastIndexOf(PathBase.PATH_SEPARATOR) + 1));
            Debug.WriteLine(filename + " succesfully deleted!");
            version.endCheckpoint();
            return true;
        }

        private static string getSharedKey(string user, string path, string password) {
            return Util.fixBase64(Hashing.ComputeHash("~" + user + path, Encoding.UTF8.GetBytes(password)));
        }

        //name can be only a folder name, shared password should not be the same as user password
        public static bool makeSharedFolder(string name, string sharedpass) {
            name = SharedPath.addTrailingSlash(name);
            string newPath = SharedPath.getSharedFolderName(name);
            if (newPath == null) {
                return false;
            }
            string newFullPath = username + newPath;
            version.startCheckpoint();
            //make sure shared doesn't exist
            if (version.DHTExists(getLsKey(username, newPath))) {
                version.endCheckpoint();
                return false;
            }
            //put new folder in for shared folder, encrypt with shared password
            version.DHTPut(getSharedKey(username, newPath, sharedpass), EncDec.Encrypt("\n", sharedpass));
            //update shared directory folder
            updateLs(newFullPath, true, SharedPath.getRoot(username));
            version.endCheckpoint();
            return true;
        }

        //enter a shared directory
        public static bool cdShared(string user, string name, string sharedpass) {
            name = SharedPath.addTrailingSlash(name);
            string newPath = SharedPath.getSharedFolderName(name);
            if (newPath == null) {
                return false;
            }
            string newFullPath = user + newPath;
            version.startCheckpoint();
            //make sure folder* does exist
            if (!version.DHTExists(getSharedKey(user, newPath, sharedpass))) {
                version.endCheckpoint();
                return false;
            }
            updateLs(newFullPath, true, SharedPath.getRoot(username));
            if (!shareMode) {
                curLocalDirectory = curDirectory;
                globalPassword = password;
                globalUsername = username;
            }
            curDirectory = newPath;
            username = user;
            password = sharedpass;
            shareMode = true;
            version.endCheckpoint();
            string list = ls();
            if (list == null) {
                return false;
            }
            return true;
        }

        public static bool exitShared() {
            if (shareMode) {
                curDirectory = curLocalDirectory;
                password = globalPassword;
                username = globalUsername;
                shareMode = false;
                return true;
            }
            return false;
        }

        public static string lsShared() {
            string user;
            string pass;
            if (shareMode) {
                user = globalUsername;
                pass = globalPassword;
            } else {
                user = username;
                pass = password;
            }
            string dir = SharedPath.getRoot(user);

            string encrypted = version.DHTGetAsString(Util.fixBase64(Hashing.ComputeHash("~" + user + ":" + dir, Encoding.UTF8.GetBytes(pass))));
            if (encrypted == null) {
                return null;
            }
            string list = EncDec.Decrypt(encrypted, pass);
            return list;
        }
    }
}
