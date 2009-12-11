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
    /// Contains a list of metadata items indexed by name
    /// </summary>
    public class MetadataList : IEnumerable<Metadata> {
        private Dictionary<string, Metadata> meta;

        /// <summary>
        /// Creates a new MetadataList
        /// </summary>
        public MetadataList() {
            meta = new Dictionary<string, Metadata>();
        }

        /// <summary>
        /// Adds a metadata item to the list
        /// </summary>
        /// <param name="metadata">The metadata item to add.</param>
        public void AddMetadata( Metadata metadata ) {
            meta.Add( metadata.Name, metadata );
        }

        /// <summary>
        /// Returns the metadata item with the specified name
        /// </summary>
        /// <param name="name">Name of the metadata item to get</param>
        /// <returns>The metadata item or null if no item is found with the specified name.</returns>
        public Metadata GetMetadata( string name ) {
            if( meta.ContainsKey( name ) ) {
                return meta[name];
            } else {
                return null;
            }
        }

        /// <summary>
        /// Returns the number of items in the metadata list.
        /// </summary>
        /// <returns>The item count</returns>
        public int Count() {
            return meta.Count;
        }

        #region IEnumerable<Metadata> Members

        /// <summary>
        /// Returns an enumerator that iterates through the metadata items
        /// </summary>
        /// <returns>An enumerator</returns>
        public IEnumerator<Metadata> GetEnumerator() {
            return meta.Values.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through the metadata items
        /// </summary>
        /// <returns>An enumerator</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return meta.Values.GetEnumerator();
        }

        #endregion
    }
}
