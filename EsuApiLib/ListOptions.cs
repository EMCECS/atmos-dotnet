// Copyright © 2011, EMC Corporation.
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

namespace EsuApiLib
{
    /// <summary>
    /// Allows you to specify extended options when listing directories or listing
    /// objects.  When using paged directory responses (limit > 0), the token
    /// used for subsequent responses will be returned through this object.
    /// </summary>
    public class ListOptions
    {
        /// <summary>
        /// Sets the maximum number of results to fetch.  Set to zero to fetch all
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// After calling ListDirectory or ListObjects, this will be set to the 
        /// token value used to fetch the next group of results.  When no more
        /// results are available, it will be null.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// When set, indicates the list of user metadata tags that should be
        /// fetched when IncludeMetadata is true.
        /// </summary>
        public List<string> UserMetadata { get; set; }

        /// <summary>
        /// When set, indicates the list of system metadata tags that should
        /// bet fetched when IncludeMetadata is true.
        /// </summary>
        public List<string> SystemMetadata { get; set; }

        /// <summary>
        /// When true, user and system metadata will be returned with the 
        /// results of the operation.
        /// </summary>
        public bool IncludeMetadata { get; set; }
    }
}
