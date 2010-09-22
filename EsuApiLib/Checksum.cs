using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Home.Andir.Cryptography;
using System.Reflection;
using System.Diagnostics;

namespace EsuApiLib
{
    /// <summary>
    /// Used to hold checksums and partial-file checksums when a file
    /// is being incrementally written.  Note that you should not directly call
    /// the update method; this will be called by the create/updateObject methods.
    /// </summary>
    public class Checksum
    {
        private long offset = 0;
        private HashAlgorithm hash;
        private Algorithm alg;
        private HashAlgorithm finalizedHash;

        /// <summary>
        /// Algorithm to use for checksumming.  Currently only
        /// SHA0 is supported (as of Atmos 1.4.0)
        /// </summary>
        public enum Algorithm
        {   
            /// <summary>
            /// Use the SHA0 Algorithm
            /// </summary>
            SHA0 = 0,
            /// <summary>
            /// Use the SHA1 Algorithm
            /// </summary>
            SHA1 = 1,
            /// <summary>
            /// Use the MD5 Algorithm
            /// </summary>
            MD5 = 2
        }

        /// <summary>
        /// Initialize a new checksum object.
        /// </summary>
        /// <param name="alg">The checksumming algorithm to use.</param>
        public Checksum(Algorithm alg)
        {
            switch (alg)
            {
                case Algorithm.SHA0:
                    hash = new SHA0();
                    break;
                case Algorithm.SHA1:
                    hash = HashAlgorithm.Create("SHA1");
                    break;
                case Algorithm.MD5:
                    hash = HashAlgorithm.Create("MD5");
                    break;
            }
            hash.Initialize();
            this.alg = alg;
        }

        /// <summary>
        /// Updates a partial checksum with the given data.
        /// </summary>
        /// <param name="data"></param>
        public void Update( ArraySegment<byte> data ) {
            finalizedHash = (HashAlgorithm)deepCopy(hash);
            hash.TransformBlock(data.Array, data.Offset, data.Count, null, 0);

            // We do this here (instead of a null transform) because the SHA0
            // implementation seems to fail for short messages if you simply
            // transform an empty block later.
            finalizedHash.TransformFinalBlock(data.Array, data.Offset, data.Count);
            offset += data.Count;
        }

        /// <summary>
        /// Returns the current checksum value in a format suitable for
        /// inclusion in the x-emc-wschecksum header.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return getHashName() + "/" + offset + "/" + getHashValue();
        }

        /// <summary>
        /// Returns the hash value in base64 format
        /// </summary>
        /// <returns>base-64 encoded bytes</returns>
        private string getHashValue()
        {
            StringBuilder digest = new StringBuilder();
            foreach (byte n in finalizedHash.Hash) digest.Append(n.ToString("x2"));
            return digest.ToString();
        }

        /// <summary>
        /// This is used as a dirty hack around C#'s hash implementation.  There isn't
        /// a good way to get the partial hash values from a HashAlgorithm object, and 
        /// the objects aren't cloneable (like in Java), so we brute force copy the
        /// object so we can obtain the partial hash value.  This is going to be
        /// slow, but it's our only option outside of implementing our own versions
        /// of the hash algorithms.
        /// </summary>
        /// <param name="source">object to copy</param>
        /// <returns>a "deep" copy of the source object</returns>
        private object deepCopy(object source)
        {
            Type t = source.GetType();
            Debug.WriteLine("Copying: " + source);

            object copy=null;
            // find the no-arg

            if (source is IntCounter)
            {
                ConstructorInfo c = t.GetConstructor(new Type[] { typeof(int) });
                FieldInfo arrayField = t.GetField("array", BindingFlags.NonPublic | BindingFlags.Instance);
                Array arr = (Array) arrayField.GetValue(source);
                copy = c.Invoke(new object[] { arr.Length });
            }
            else
            {
                ConstructorInfo c = t.GetConstructor(System.Type.EmptyTypes);
                if (c == null)
                {
                    Debug.WriteLine("Object is not copyable: " + source);
                    return source;
                }
                copy = (object)c.Invoke(new object[0]);
            }
            Console.WriteLine("new object: " + copy);
            foreach (FieldInfo fi in t.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                Console.WriteLine("Field: " + fi.Name);
                object value = fi.GetValue(source);
                if (value is Array)
                {
                    // Duplicate the array
                    Array newvalue = Array.CreateInstance(value.GetType().GetElementType(), ((Array)value).Length);
                    Array.Copy((Array)value, newvalue, newvalue.Length);
                    Console.WriteLine("Array copied");
                    value = newvalue;
                }
                else if (value == null || value.GetType().IsPrimitive)
                {
                    // Ignore
                }
                else
                {
                    value = deepCopy(value);
                }
                Console.WriteLine("Setting " + fi.Name + " to " + value);
                fi.SetValue(copy, value);
            }

            return copy;
        }

        /// <summary>
        /// Returns the name of the hash algorithm, e.g. SHA1
        /// </summary>
        private string getHashName()
        {
            switch (alg)
            {
                case Algorithm.SHA0:
                    return "SHA0";
                case Algorithm.SHA1:
                    return "SHA1";
                case Algorithm.MD5:
                    return "MD5";
                default:
                    throw new Exception("Unknown hash algorithm: " + alg);
            }
        }
    }
}
