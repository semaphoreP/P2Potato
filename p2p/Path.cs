using System;
using System.Text;
using System.Text.RegularExpressions;

namespace P2Potato {
    abstract class PathBase {
        public static char PATH_SEPARATOR = '/';
        public static string CUR_DIR = ".";
        public static string PREV_DIR = "..";

        /// <summary>
        /// Adds a trailing PATH_SEPARATOR to the path if one is not present.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static string addTrailingSlash(string dir) {
            if (dir == null || dir.Length == 0) {
                return null;
            }
            return dir[dir.Length - 1] != PATH_SEPARATOR ? dir + PATH_SEPARATOR : dir;
        }
    }

    class SharedPath : PathBase {
        public static string ROOT = "://";
        public static string ROOT_PREFIX = ":/";

        /// <summary>
        /// Compresses a path based upon the PREV_DIR being able to be
        /// compressed to back one level, and removal of CUR_DIR.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string compressPath(string path) {
            string[] arr = Regex.Split(path, ROOT);
            string filepath = arr[0] + ROOT_PREFIX;
            arr = arr[1].Split(PATH_SEPARATOR);
            for (int i = 0; i < arr.Length; i++) {
                if (arr[i].Equals(PREV_DIR)) {
                    if (filepath.Length == 0) {
                        return null;
                    }
                    filepath = filepath.Substring(0, filepath.LastIndexOf(PATH_SEPARATOR));
                } else if (!arr[i].Equals(CUR_DIR)) {
                    filepath += PATH_SEPARATOR + arr[i];
                }
            }
            return filepath;
        }

        /// <summary>
        /// Returns the absolute path based on the current directory.
        /// </summary>
        /// <param name="curDirectory">Absolute path to current directory</param>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string getAbsolutePath(string curDirectory, string foldername) {
            // Validate name can't be an "absolute" path
            if (foldername == "/" || foldername == null) {
                return null;
            }
            if (foldername.StartsWith(ROOT)) {
                return compressPath(foldername);
            } else {
                return compressPath(curDirectory + foldername);
            }
        }

        public static string getSharedFolderName(string folder) {
            // Validate name can't be an "absolute" path
            if (folder == "/" || folder == null) {
                return null;
            }
            if (folder[folder.Length - 1] != PATH_SEPARATOR) {
                folder += PATH_SEPARATOR;
            }
            return ROOT + folder;
        }

        public static string getRoot(string name) {
            return name == null ? null : name + ROOT;
        }
    }

    class SingleUserPath : PathBase {
        public static string ROOT = "/";

        /// <summary>
        /// Compresses a path based upon the ".." being able to be
        /// compressed to back one level.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string compressPath(string path) {
            string[] arr = path.Split(PATH_SEPARATOR);
            string filepath = "";
            for (int i = 1; i < arr.Length; i++) {
                if (arr[i].Equals(PREV_DIR)) {
                    if (filepath.Length == 0) {
                        return null;
                    }
                    filepath = filepath.Substring(0, filepath.LastIndexOf(PATH_SEPARATOR));
                } else if (!arr[i].Equals(CUR_DIR)) {
                    filepath += PATH_SEPARATOR + arr[i];
                }
            }
            return filepath;
        }

        /// <summary>
        /// Returns the absolute path based on the current directory.
        /// </summary>
        /// <param name="curDirectory">Absolute path to current directory</param>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string getAbsolutePath(string curDirectory, string input) {
            if (input.StartsWith(ROOT)) {
                return compressPath(input);
            } else {
                return compressPath(curDirectory + input);
            }
        }
    }
}
