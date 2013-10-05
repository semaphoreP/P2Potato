using System;
using System.Text;
using System.IO;
using Isis;

namespace P2Potato {
    /// <summary>
    /// Allows creation of versioned data which uses a timestamp and address to determine
    /// whether or not data is "newer" than other data.
    /// </summary>
    class Version {
        private const int NUM_BYTES_TIMESTAMP = sizeof(long); // 64-bit number
        private const int NUM_BYTES_ADDRESS = sizeof(int); // 32-bit number

        public const int DATA_OFFSET = NUM_BYTES_TIMESTAMP + NUM_BYTES_ADDRESS;

        private const long VALID_RANGE = TimeSpan.TicksPerHour;

        private readonly Group group;
        private readonly byte[] address;
        private byte[] timestamp;

        public Version(Group group, int address) {
            this.group = group;
            this.address = BitConverter.GetBytes(address);
            this.timestamp = null;
        }

        /// <summary>
        /// Call this whenever the timestamp functionality needs to be reset.
        /// </summary>
        public void startCheckpoint() {
            if (this.timestamp != null) {
                throw new InvalidOperationException("Cannot start a checkpoint in a checkpoint");
            }
            this.timestamp = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
        }

        /// <summary>
        /// Ends a checkpoint. Only truly for debugging purposes.
        /// </summary>
        public void endCheckpoint() {
            if (this.timestamp == null) {
                throw new InvalidOperationException("Cannot end a checkpoint that isn't started");
            }
            this.timestamp = null;
        }

        /// <summary>
        /// Treats data as invalid if it is shorter than the versioning prefix.
        /// </summary>
        /// <param name="versionedData"></param>
        /// <returns></returns>
        private static bool isInvalidData(byte[] versionedData) {
            return versionedData == null || versionedData.Length < DATA_OFFSET;
        }

        private static byte[] addVersionData(byte[] timestamp, byte[] address, byte[] data) {
            byte[] newData = new byte[DATA_OFFSET + data.Length];
            Buffer.BlockCopy(timestamp, 0, newData, 0, NUM_BYTES_TIMESTAMP);
            Buffer.BlockCopy(address, 0, newData, NUM_BYTES_TIMESTAMP, NUM_BYTES_ADDRESS);
            Buffer.BlockCopy(data, 0, newData, DATA_OFFSET, data.Length);
            return newData;
        }

        private static Tuple<long, int> getVersionData(byte[] versionedData) {
            Tuple<byte[], byte[]> bytesTuple = getVersionDataAsBytes(versionedData);
            if (bytesTuple == null) {
                return null;
            }
            return new Tuple<long, int>(BitConverter.ToInt64(bytesTuple.Item1, 0), BitConverter.ToInt32(bytesTuple.Item2, 0));
        }

        /// <summary>
        /// Returns the version data as a tuple of timestamp and address.
        /// </summary>
        /// <param name="versionedData"></param>
        /// <returns></returns>
        private static Tuple<byte[], byte[]> getVersionDataAsBytes(byte[] versionedData) {
            if (isInvalidData(versionedData)) {
                return null;
            }
            byte[] timestamp = new byte[NUM_BYTES_TIMESTAMP];
            byte[] address = new byte[NUM_BYTES_ADDRESS];
            Buffer.BlockCopy(versionedData, 0, timestamp, 0, NUM_BYTES_TIMESTAMP);
            Buffer.BlockCopy(versionedData, NUM_BYTES_TIMESTAMP, address, 0, NUM_BYTES_ADDRESS);
            return new Tuple<byte[], byte[]>(timestamp, address);
        }

        /// <summary>
        /// Convenience function for extracting the true data.
        /// </summary>
        /// <param name="versionedData"></param>
        /// <returns></returns>
        public static byte[] removeVersionData(byte[] versionedData) {
            if (isInvalidData(versionedData)) {
                return null;
            }
            int dataLength = versionedData.Length - DATA_OFFSET;
            byte[] data = new byte[dataLength];
            Buffer.BlockCopy(versionedData, DATA_OFFSET, data, 0, dataLength);
            return data;
        }

        public Tuple<object, Tuple<long, int>> DHTGetWithVersionData(object key) {
            byte[] versionedData = (byte[])group.DHTGet(key);
            if (isInvalidData(versionedData)) {
                return null;
            }
            return new Tuple<object, Tuple<long, int>>(Msg.BArrayToObjects(removeVersionData(versionedData))[0], getVersionData(versionedData));
        }

        /// <summary>
        /// Convenience function for a known byte array.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public byte[] DHTGetAsBytes(object key) {
            byte[] versionedData = (byte[])group.DHTGet(key);
            if (isInvalidData(versionedData)) {
                return null;
            }
            return removeVersionData(versionedData);
        }

        /// <summary>
        /// Convenience function for a known string type.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string DHTGetAsString(object key) {
            byte[] bytes = DHTGetAsBytes(key);
            return bytes == null ? null : Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// DHTGet for version timestamped data.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object DHTGet(object key) {
            byte[] bytes = DHTGetAsBytes(key);
            return bytes == null ? null : Msg.BArrayToObjects(bytes)[0];
        }

        /// <summary>
        /// Convenience function for checking existence of a name.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool DHTExists(object key) {
            byte[] values = DHTGetAsBytes(key);
            return values != null;
        }

        /// <summary>
        /// DHTPut for version timestamped data.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void DHTPut(object key, object value) {
            DHTPut(key, Msg.toBArray(value));
        }

        /// <summary>
        /// Convenience function for string values.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void DHTPut(object key, string value) {
            DHTPut(key, Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        /// DHTPut for version timestamped data.
        /// <tt>startCheckpoint</tt> must be called before this
        /// function.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void DHTPut(object key, byte[] value) {
            if (timestamp == null) {
                throw new InvalidOperationException("P2Potato.Version.DHTPut: Did not start checkpoint");
            }
            byte[] versionedData = addVersionData(timestamp, address, value);
            group.DHTPut(key, versionedData);
        }

        /// <summary>
        /// DHTRemove for version timestamped data.
        /// </summary>
        /// <param name="key"></param>
        public void DHTRemove(object key) {
            DHTPut(key, new byte[0]);
        }

        /// <summary>
        /// Writes the latest of two files to a specified filepath.
        /// Prefix version data is used to choose "newer" file.
        /// Later timestamps win and higher IPs win.
        /// </summary>
        /// <param name="versionedData"></param>
        /// <param name="filepath"></param>
        public bool writeLatestFile(byte[] versionedData, string filepath) {
            byte[] oldVersionedData = new byte[DATA_OFFSET];
            int len;

            // If there is no metadata in the file, discard it. It must be old or byzantine
            if (isInvalidData(versionedData)) {
                return false;
            }
            var newMetadata = getVersionData(versionedData);
            if (File.Exists(filepath)) {
                // Get the first timestamp and address if available.
                using (FileStream filestream = File.OpenRead(filepath)) {
                    len = filestream.Read(oldVersionedData, 0, DATA_OFFSET);
                }

                // If file is not too short and timestamp is old / address is incorrect, end early.
                if (!isInvalidData(oldVersionedData)) {
                    var oldMetadata = getVersionData(oldVersionedData);
                    if (newMetadata.Item1 < oldMetadata.Item1 || (newMetadata.Item1 == oldMetadata.Item1 && newMetadata.Item1 <= oldMetadata.Item2)) {
                        return false;
                    }
                }
            }
            //Debug.WriteLine(newMetadata.Item1);
            // Data is byzantine? The time is way too far in the future.
            if (DateTime.UtcNow.Ticks + VALID_RANGE < newMetadata.Item1) {
                return false;
            }

            // If data in message is length 0, that means "delete"
            if (removeVersionData(versionedData).Length == 0) {
                Debug.WriteLine("P2Potato.Platform.startP2Potato: Deleting file " + filepath);
                File.Delete(filepath);
            } else {
                using (FileStream filestream = File.OpenWrite(filepath)) {
                    filestream.Write(versionedData, 0, versionedData.Length);
                }
            }
            return true;
        }
    }
}
