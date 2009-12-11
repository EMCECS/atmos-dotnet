// Copyright © 2008, EMC Corporation.
// Redistribution and use in source and binary forms, with or without modification, 
// are permitted provided that the following conditions are met:
//
//     + Redistributions of source code must retain the above copyright notice, 
//       this list of conditions and the following disclaimer.
//     + Redistributions in binary form must reproduce the above copyright 
//       notice, this list of conditions and the following disclaimer in the 
//       documentation and/or other materials provided with the distribution.
//     + The name of EMC Corporation may not be used to endorse or promote 
//       products derived from this software without specific prior written 
//       permission.
//
//      THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
//      "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED 
//      TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR 
//      PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS 
//      BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
//      CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
//      SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
//      INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
//      CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
//      ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
//      POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Text;

namespace EsuApiLib {
    /// <summary>
    /// An Access Control List (ACL) is a collection of Grants that assign privileges to users and/or groups. 
    /// </summary>
    public class Acl : IEnumerable<Grant> {
        // Should be a HashSet, but not supported until
        // .Net 3.5.  Use a List for 2.0 compatability.
        private List<Grant> grants = new List<Grant>();

        /// <summary>
        /// Adds a Grant to the ACL
        /// </summary>
        /// <param name="g">The Grant to add</param>
        public void AddGrant( Grant g ) {
            if ( !grants.Contains( g ) ) {
                grants.Add( g );
            }
        }

        /// <summary>
        /// Removes a Grant from the ACL
        /// </summary>
        /// <param name="g">The Grant to remove</param>
        public void RemoveGrant( Grant g ) {
            grants.Remove( g );
        }

        /// <summary>
        /// Returns the number of Grants in the ACL.
        /// </summary>
        /// <returns>The ACL's grant count.</returns>
        public int Count() {
            return grants.Count;
        }

        /// <summary>
        /// Removes all grants from the ACL
        /// </summary>
        public void Clear() {
            grants.Clear();
        }

        /// <summary>
        /// Determines if two ACLs are equal.  Two ACLs are equal if they contain the same
        /// set of Grants.
        /// </summary>
        /// <param name="obj">The object to compare this ACL to</param>
        /// <returns>True if the ACLs are equal.</returns>
        public override bool Equals( object obj ) {
            Acl acl2 = (Acl)obj;
            if ( acl2.grants.Count != this.grants.Count ) {
                return false;
            }
            foreach ( Grant g in acl2.grants ) {
                if ( !grants.Contains( g ) ) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets a hash code for the ACL
        /// </summary>
        /// <returns>The hash code (from ToString().GetHashCode())</returns>
        public override int GetHashCode() {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// Returns the ACL's grants in a String form.
        /// </summary>
        /// <returns>The ACL in string form (grantee=permission, grantee2=permission2, ...)</returns>
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            foreach( Grant g in grants ) {
                if( sb.Length > 0 ) {
                    sb.Append( ", " );
                }
                sb.Append( g.ToString() );
            }
            return sb.ToString();
        }


        #region IEnumerable<Grant> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection. 
        /// </summary>
        /// <returns>An IEnumerator object that can be used to iterate through the collection. </returns>
        public IEnumerator<Grant> GetEnumerator() {
            return grants.GetEnumerator();
        }

        #endregion


        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection. 
        /// </summary>
        /// <returns>An IEnumerator object that can be used to iterate through the collection. </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return grants.GetEnumerator();
        }

        #endregion
    }
}
