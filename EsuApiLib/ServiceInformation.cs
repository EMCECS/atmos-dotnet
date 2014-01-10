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
using System.Linq;
using System.Text;

namespace EsuApiLib
{
    /// <summary>
    /// Used to return the response from the GetServiceInformation call.
    /// Note that Atmos systems prior to 2.0.3 might not return a feature list.
    /// </summary>
    public class ServiceInformation
    {
        /// <summary>
        /// REST objectspace (Atmos REST is enabled).
        /// </summary>
	    public const string OBJECT = "object";

        /// <summary>
        /// REST namespace (Atmos REST is enabled).
        /// </summary>
	    public const string NAMESPACE = "namespace";

        /// <summary>
        /// utf-8 encoding support for headers (1.4.2+).
        /// </summary>
	    public const string UTF_8 = "utf-8";

        /// <summary>
        /// browser compatibility enhancements (2.0.3+).
        /// </summary>
	    public const string BROWSER_COMPAT = "browser-compat";

        /// <summary>
        /// REST keypools (disabled by default).
        /// </summary>
	    public const string KEY_VALUE = "key-value";

        /// <summary>
        /// hard-linking feature (disabled by default).
        /// </summary>
	    public const string HARDLINK = "hardlink";

        /// <summary>
        /// object query support (disabled and not recommended due to slow performance).
        /// </summary>
	    public const string QUERY = "query";

        /// <summary>
        /// object versioning support.
        /// </summary>
        public const string VERSIONING = "versioning";

        /// <summary>
        /// The version of Atmos in the form of major.minor.patch, e.g. 1.4.0
        /// </summary>
        public string AtmosVersion{set;get;}

        /// <summary>
        /// Indicates whether the Atmos system supports utf-8 encoded metadata.
        /// </summary>
        public bool UnicodeMetadataSupported { set; get; }

        /// <summary>
        /// Holds the set of features supported by the Atmos System. I.e. <see cref="UTF_8"/>, etc.
        /// </summary>
        public HashSet<string> Features { set; get; }

        /// <summary>
        /// Creates a new ServiceInformation object.
        /// </summary>
        public ServiceInformation()
        {
            Features = new HashSet<string>();
        }

        /// <summary>
        /// Adds a feature to the list of supported features.
        /// </summary>
        /// <param name="feature"></param>
        public void AddFeature(string feature)
        {
            Features.Add(feature);
        }

        /// <summary>
        /// Checks to see if a feature is supported.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        public bool HasFeature(string feature)
        {
            return Features.Contains(feature);
        }
    }
}
