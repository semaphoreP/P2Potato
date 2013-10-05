using System;

namespace P2Potato {
    /// <summary>
    /// Global Debugging printing which can be turned
    /// off by manually changing the debugOn variable.
    /// </summary>
    static class Debug {
        public static bool debugOn = false;

        public static void WriteLine() {
            if (debugOn) {
                Console.WriteLine();
            }
        }

        public static void WriteLine(object s) {
            if (debugOn) {
                Console.WriteLine(s);
            }
        }

        public static void Write(object s) {
            if (debugOn) {
                Console.Write(s);
            }
        }
    }
}
