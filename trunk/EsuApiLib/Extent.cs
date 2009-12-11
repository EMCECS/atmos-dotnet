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
    /// An extent specifies a portion of an object to read or write. It contains a starting offset and a number of bytes to read or write. 
    /// </summary>
    public class Extent {
        /// <summary>
        /// A static Extent that represents an entire object's contents.
        /// </summary>
        public static Extent ALL_CONTENT = new Extent( -1, -1 );


        private long offset;

        /// <summary>
        /// The starting offset of the extent 
        /// </summary>
        public long Offset {
            get { return offset; }
        }

        private long size;

        /// <summary>
        /// The size of the extent in bytes
        /// </summary>
        public long Size {
            get { return size; }
        }
	
        /// <summary>
        /// Creates a new extent 
        /// </summary>
        /// <param name="offset">The starting offset in the object in bytes, starting with 0. Use -1 to represent the entire object.</param>
        /// <param name="size">The number of bytes to transfer. Use -1 to represent the entire object.</param>
        public Extent( long offset, long size ) {
            this.offset = offset;
            this.size = size;
        }

        /// <summary>
        /// Compares two objects for equality.  Extents are equal if both their
        /// offsets and sizes are equal.
        /// </summary>
        /// <param name="obj">Object to compare with this object</param>
        /// <returns>True if they are equal</returns>
        public override bool Equals( object obj ) {
            Extent e2 = (Extent)obj;
            return e2.offset == offset && e2.size == size;
        }

        /// <summary>
        /// Returns a hash code for this object
        /// </summary>
        /// <returns>the object's hash code.</returns>
        public override int GetHashCode() {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// Returns a String representation of the extent.
        /// </summary>
        /// <returns>the extent as a string.</returns>
        public override string ToString() {
            return "Extent: offset: " + offset + " size: " + size;
        } 
	
    }
}
