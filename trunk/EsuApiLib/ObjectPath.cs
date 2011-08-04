using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace EsuApiLib {
    /// <summary>
    /// This class is used to identify an object by a 
    /// path, e.g. /groceries/fruit/banana.txt
    /// </summary>
    public class ObjectPath : Identifier {
        /// <summary>
        /// The regular expression that checks whether identifiers are valid.
        /// </summary>

        private string path;

        /// <summary>
        /// Creates a new object identifier
        /// </summary>
        /// <param name="path">The identifier string</param>
        public ObjectPath( string path ) {
            this.path = path;
        }

        /// <summary>
        /// Gets the name of the file or directory (the last component on the path)
        /// </summary>
        /// <returns>the file name</returns>
        public string GetFileName()
        {
            string x = path;
            if (path.EndsWith("/"))
            {
                x = path.Substring(0, path.Length - 1);
            }
            int pos = x.LastIndexOf('/');
            pos++; // skip "/"
            return x.Substring(pos, x.Length - pos);
        }

        /// <summary>
        /// Returns this object identifier as a string
        /// </summary>
        /// <returns>the ID as a string</returns>
        public override string ToString() {
            return path;
        }

        /// <summary>
        /// Checks to see if two identifiers are equal
        /// </summary>
        /// <param name="obj">The other object to check</param>
        /// <returns>true if the identifier strings are equal.</returns>
        public override bool Equals( object obj ) {
            return path.Equals( obj.ToString() );
        }

        /// <summary>
        /// Returns the object hash code
        /// </summary>
        /// <returns>the hash code</returns>
        public override int GetHashCode() {
            return path.GetHashCode();
        }

        /// <summary>
        /// Returns true if the object path refers to 
        /// a directory.
        /// </summary>
        /// <returns></returns>
        public bool IsDirectory() {
            return path.EndsWith("/");
        }
    }
}
