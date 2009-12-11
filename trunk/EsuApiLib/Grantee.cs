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
    /// A grantee represents a user or group that recieves a permission grant. 
    /// </summary>
    public class Grantee {
        /// <summary>
        /// Static instance that represents the special group 'other'
        /// </summary>
        public static readonly Grantee OTHER = new Grantee( "other", GRANTEE_TYPE.GROUP );

        private string name;

        /// <summary>
        /// The grantee's name
        /// </summary>
        public string Name {
            get { return name; }
        }

        private GRANTEE_TYPE granteeType;

        /// <summary>
        /// The grantee's type.
        /// </summary>
        public GRANTEE_TYPE GranteeType {
            get { return granteeType; }
            set { granteeType = value; }
        }
	
	    /// <summary>
	    /// The grantee type: a user or a group.
	    /// </summary>
        public enum GRANTEE_TYPE { 
            /// <summary>
            /// A grant to a user
            /// </summary>
            USER, 
            /// <summary>
            /// A grant to a group
            /// </summary>
            GROUP };

        /// <summary>
        /// Creates a new grantee.
        /// </summary>
        /// <param name="name">The grantee's name</param>
        /// <param name="granteeType">The grantee's type.</param>
        public Grantee( string name, GRANTEE_TYPE granteeType ) {
            this.name = name;
            this.granteeType = granteeType;
        }

        /// <summary>
        /// Returns this grantee object as a string
        /// </summary>
        /// <returns>The grantee in string form</returns>
        public override string ToString() {
            return name;
        }

        /// <summary>
        /// Determines whether two grantees are equal.  They must have both the same name and type.
        /// </summary>
        /// <param name="obj">The object to compare this to</param>
        /// <returns>True if these grantees are equal</returns>
        public override bool Equals( object obj ) {
            Grantee g2 = (Grantee)obj;
            return g2.name.Equals( name ) && g2.granteeType.Equals( granteeType );
        }

        /// <summary>
        /// Returns a hash code for the grantee.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() {
            return name.GetHashCode();
        }
    }
}
