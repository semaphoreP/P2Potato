using System.Collections.Generic;

namespace P2Potato {
    /// <summary>
    /// This is a simple write-through cache which takes in handlers for what to do
    /// if the get or put is not in the cache.
    /// </summary>
    class Cache<TKey, TValue> {

        public delegate TValue GetHandler(TKey key);
        public delegate bool PutHandler(TKey key, TValue value);
        public delegate bool RemoveHandler(TKey key);

        private Dictionary<TKey, TValue> cache;
        private LinkedList<TKey> leastRecent;

        private int itemCount;
        private int maxSize;
        private GetHandler getHandler;
        private PutHandler putHandler;
        private RemoveHandler removeHandler;

        /// <summary>
        /// Creates a new cache wit the correct handlers and size.
        /// </summary>
        /// <param name="maxSize">Max size of the cache in terms of number of keys</param>
        /// <param name="getHandler"></param>
        /// <param name="putHandler"></param>
        /// <param name="removeHandler"></param>
        public Cache(int maxSize, GetHandler getHandler, PutHandler putHandler, RemoveHandler removeHandler) {
            itemCount = 0;
            this.maxSize = maxSize;
            this.getHandler = getHandler;
            this.putHandler = putHandler;
            this.removeHandler = removeHandler;
            cache = new Dictionary<TKey, TValue>(this.maxSize);
            leastRecent = new LinkedList<TKey>();
        }

        /// <summary>
        /// Gets the key's value. Returns null if fails.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue get(TKey key) {
            if (cache.ContainsKey(key)) {
                leastRecent.Remove(key);
                leastRecent.AddLast(key);
                return cache[key];
            } else {
                TValue value = getHandler(key);
                if (value != null) {
                    // Bring into cache
                    if (itemCount == maxSize) {
                        cache.Remove(leastRecent.First.Value);
                        leastRecent.RemoveFirst();
                    } else {
                        itemCount++;
                    }
                    cache[key] = value;
                    leastRecent.AddLast(key);
                }
                return value;
            }
        }

        /// <summary>
        /// Removes a key from the cache and disk.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool remove(TKey key) {
            if (removeHandler(key)) {
                cache.Remove(key);
                itemCount--;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Puts a key into the cache and on disk.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool put(TKey key, TValue value) {
            if (putHandler(key, value)) {
                if (!cache.ContainsKey(key)) {
                    if (itemCount == maxSize) {
                        cache.Remove(leastRecent.First.Value);
                        leastRecent.RemoveFirst();
                    } else {
                        itemCount++;
                    }
                }
                cache[key] = value;
                leastRecent.AddFirst(key);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Flushes the cache.
        /// </summary>
        public void flush() {
            cache.Clear();
            itemCount = 0;
        }
    }
}
