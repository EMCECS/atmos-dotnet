using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EsuApiLib
{
    /// <summary>
    /// Represents a key-pool/key combination as an object identifier.
    /// </summary>
    public class ObjectKey : Identifier
    {
        /// <summary>
        /// The key-pool (pool of keys) that holds this key.
        /// </summary>
        public string pool { get; private set; }

        /// <summary>
        /// The key value (user-defined identifier of an object).
        /// </summary>
        public string key { get; private set; }

        /// <summary>
        /// Creates a new object key with the specified key-pool and key value.
        /// </summary>
        /// <param name="pool">The key-pool (pool of keys) that holds this object key.</param>
        /// <param name="key">The key value (user-defined identifier for an object).</param>
        public ObjectKey(string pool, string key)
        {
            this.pool = pool;
            this.key = key;
        }

        /// <summary>
        /// Returns the identifier as a string. Includes the identifier type and the key-pool and key value.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("ObjectKey{{pool={0}, key={1}}}", pool, key);
        }

        /// <summary>
        /// Compares this identifier to another and returns true if they are equal.
        /// Both objects must be ObjectKeys and have the same key-pool and key value.
        /// </summary>
        /// <param name="obj">The other object to compare against this object.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            ObjectKey ok = obj as ObjectKey;
            if (ok == null)
            {
                return false;
            }

            return pool == ok.pool && key == ok.key;
        }

        /// <summary>
        /// Generates a hashcode for this object. Includes the key-pool and key value.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = 1;
            hash = hash * 17 + pool.GetHashCode();
            hash = hash * 31 + key.GetHashCode();
            return hash;
        }
    }
}
