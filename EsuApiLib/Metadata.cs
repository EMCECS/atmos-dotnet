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
    /// Encapsulates a piece of object metadata 
    /// </summary>
    public class Metadata {
        private string name;

        /// <summary>
        /// The name of the metadata object.  Immutable.
        /// </summary>
        public string Name {
            get { return name; }
        }

        private string val;

        /// <summary>
        /// The metadata object's value 
        /// </summary>
        public string Value {
            get { return val; }
            set { val = value; }
        }

        private bool listable;

        /// <summary>
        /// Determines whether this metadata object is listable.
        /// </summary>
        public bool Listable {
            get { return listable; }
            set { listable = value; }
        }
	
	
        /// <summary>
        /// Creates a new piece of metadata 
        /// </summary>
        /// <param name="name">The name of the metadata (e.g. 'Title')</param>
        /// <param name="value">The metadata value (e.g. 'Hamlet')</param>
        /// <param name="listable">Whether to make the value listable. You can query objects with a specific listable metadata tag using the listObjects method in the API.</param>
        public Metadata( string name, string value, bool listable ) {
            this.name = name;
            this.val = value;
            this.listable = listable;
        }

        /// <summary>
        /// Returns the metadata in the form of name=value
        /// </summary>
        /// <returns>metadata as string</returns>
        public override string ToString()
        {
            return name + "=" + val;
        }
    }
}
