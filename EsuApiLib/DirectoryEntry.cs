﻿// Copyright © 2008, EMC Corporation.
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
    /// Contains information about an entry in a directory
    /// listing.
    /// </summary>
    public class DirectoryEntry {
        private ObjectPath path;
        private ObjectId id;
        private String type;
        private MetadataList systemMetadata;
        private MetadataList userMetadata;

        /// <summary>
        /// Gets the object path for this entry
        /// </summary>
        public ObjectPath Path {
            get {
                return path;
            }
            set {
                path = value;
            }
        }

        /// <summary>
        /// Gets this entry's object id.
        /// </summary>
        public ObjectId Id {
            get {
                return id;
            }
            set {
                id = value;
            }
        }

        /// <summary>
        /// Gets the type of this object (regular, 
        /// directory, link, etc)
        /// </summary>
        public string Type {
            get {
                return type;
            }
            set {
                type = value;
            }
        }

        /// <summary>
        /// If ListDirectory is called with IncludeMetadata=true, this
        /// field will be populated with the object's system metadata.
        /// </summary>
        public MetadataList SystemMetadata
        {
            get
            {
                return systemMetadata;
            }
            set
            {
                systemMetadata = value;
            }
        }

        /// <summary>
        /// If ListDirectory is called with IncludeMetadata=true, this
        /// field will be populated with the object's user metadata.
        /// </summary>
        public MetadataList UserMetadata
        {
            get
            {
                return userMetadata;
            }
            set
            {
                userMetadata = value;
            }
        }
   }
}
