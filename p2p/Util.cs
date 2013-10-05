using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace P2Potato {
    static class Util {
        /// <summary>
        /// Fixes the issue where SingleUserPath.PATH_SEPARATOR should not be used in the hash.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string fixBase64(string str) {
            return str.Replace(SingleUserPath.PATH_SEPARATOR, '-');
        }

        /// <summary>
        /// Fixes the issue where SingleUserPath.PATH_SEPARATOR should not be used in the hash.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string unfixBase64(string str) {
            return str.Replace('-', SingleUserPath.PATH_SEPARATOR);
        }
    }
}
