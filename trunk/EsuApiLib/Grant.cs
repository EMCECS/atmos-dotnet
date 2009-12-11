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
    /// Used to grant a permission to a grantee (a user or group) 
    /// </summary>
    public class Grant {
        private Grantee grantee;

        /// <summary>
        /// The recipient of the grant
        /// </summary>
        public Grantee Grantee {
            get { return grantee; }
        }

        private string permission;

        /// <summary>
        /// The permission granted to the grantee
        /// </summary>
        public string Permission {
            get { return permission; }
            set { permission = value; }
        }
	
	    /// <summary>
	    /// Creates a new grant
	    /// </summary>
	    /// <param name="grantee">The recipient of the grant</param>
	    /// <param name="permission">The permissions granted</param>
        public Grant( Grantee grantee, string permission ) {
            this.grantee = grantee;
            this.permission = permission;
        }

        /// <summary>
        /// Returns this grant as a string
        /// </summary>
        /// <returns>The grant in string form (grantee=permission)</returns>
        public override string ToString() {
            return grantee + "=" + permission;
        }

        /// <summary>
        /// Returns true if two grants are equal.  The grantee and permissions must be the same.
        /// </summary>
        /// <param name="obj">The object to test for equality</param>
        /// <returns>True if the grants are equal</returns>
        public override bool Equals( object obj ) {
            Grant g2 = (Grant)obj;
            return g2.grantee.Equals( grantee ) && g2.permission.Equals( permission );
        }

        /// <summary>
        /// Returns a hash code for this object.
        /// </summary>
        /// <returns>the hash code</returns>
        public override int GetHashCode() {
            return grantee.GetHashCode();
        }
    }
}
