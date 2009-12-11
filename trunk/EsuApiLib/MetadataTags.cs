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
    /// The MetadataTags class contains a collection of metadata tags indexed by name.
    /// </summary>
    public class MetadataTags : IEnumerable<MetadataTag> {
        private Dictionary<string,MetadataTag> tags;

        /// <summary>
        /// Creates a new MetadataTags object.
        /// </summary>
        public MetadataTags() {
            tags = new Dictionary<string, MetadataTag>();
        }

        /// <summary>
        /// Adds a tag to the set of tags 
        /// </summary>
        /// <param name="tag">The tag to add</param>
        public void AddTag( MetadataTag tag ) {
            if ( !tags.ContainsKey( tag.Name ) ) {
                tags.Add( tag.Name, tag );
            }
        }

        /// <summary>
        /// Removes a tag from the set of tags 
        /// </summary>
        /// <param name="tag">The tag to remove</param>
        public void RemoveTag( MetadataTag tag ) {
            if ( tags.ContainsKey( tag.Name ) ) {
                tags.Remove( tag.Name );
            }
        }

        /// <summary>
        /// Gets a tag from the set with the given name 
        /// </summary>
        /// <param name="name">The name of the tag to get.</param>
        /// <returns>The requested tag or null if the tag is not found.</returns>
        public MetadataTag GetTag( string name ) {
            return tags[name];
        }

        /// <summary>
        /// Returns true if the requested tag is in the set.
        /// </summary>
        /// <param name="tag">The tag to look for.</param>
        /// <returns>True if the tag is found.</returns>
        public bool Contains( MetadataTag tag ) {
            return tags.ContainsKey( tag.Name );
        }

        /// <summary>
        /// Returns true if a tag with the given name exists in the set.
        /// </summary>
        /// <param name="tagName">The name to search for.</param>
        /// <returns>True if a tag is found with the given name</returns>
        public bool Contains( string tagName ) {
            return tags.ContainsKey( tagName );
        }
        
        /// <summary>
        /// Returns the number of tags in this set 
        /// </summary>
        /// <returns>The tag count</returns>
        public int Count() {
            return tags.Count;
        }

        #region IEnumerable<MetadataTag> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection. 
        /// </summary>
        /// <returns>An IEnumerator object that can be used to iterate through the collection. </returns>
        public IEnumerator<MetadataTag> GetEnumerator() {
            return tags.Values.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection. 
        /// </summary>
        /// <returns>An IEnumerator object that can be used to iterate through the collection. </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return tags.Values.GetEnumerator();
        }

        #endregion

    }
}
