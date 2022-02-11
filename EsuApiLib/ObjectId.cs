// Copyright © 2014, EMC Corporation.
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
using System.Text.RegularExpressions;

namespace EsuApiLib {
    /// <summary>
    /// Encapsulates an ESU object identifier. Performs validation
    /// upon construction to ensure that the identifier format is correct.
    /// </summary>
    public class ObjectId : Identifier {
        /// <summary>
        /// The regular expression that checks whether identifiers are valid.
        /// </summary>
        private static readonly Regex ID_FORMAT = new Regex("^[0-9a-f-]{44,}$");

        private string id;

        /// <summary>
        /// Creates a new object identifier
        /// </summary>
        /// <param name="id">The identifier string</param>
        public ObjectId( string id ) {
            if ( !ID_FORMAT.IsMatch( id ) ) {
                throw new EsuException( id + " is not a valid object id" );
            }
            this.id = id;
        }

        /// <summary>
        /// Returns this object identifier as a string
        /// </summary>
        /// <returns>the ID as a string</returns>
        public override string ToString() {
            return id;
        }

        /// <summary>
        /// Checks to see if two identifiers are equal
        /// </summary>
        /// <param name="obj">The other object to check</param>
        /// <returns>true if the identifier strings are equal.</returns>
        public override bool Equals( object obj ) {
            return id.Equals( obj.ToString() );
        }

        /// <summary>
        /// Returns the object hash code
        /// </summary>
        /// <returns>the hash code</returns>
        public override int GetHashCode() {
            return id.GetHashCode();
        }
    }
}
