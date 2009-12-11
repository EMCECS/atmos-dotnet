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
    /// Base ESU exception class that is thrown from the API methods.  Contains an
    /// error code that can be mapped to the standard ESU error codes.
    /// </summary>
    public class EsuException : Exception {
        private int code;

        /// <summary>
        /// The error code returned from the ESU server
        /// </summary>
        public int Code {
            get { return code; }
            set { code = value; }
        }

        /// <summary>
        /// Creates a new ESU Exception with the given message.  The error code
        /// will be set to 0.
        /// </summary>
        /// <param name="message">The error message</param>
        public EsuException( string message ):base( message ) {
            this.code = 0;
        }

        /// <summary>
        /// Creates a new ESU exception with the given message and error code.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="code">The error code from the ESU server</param>
        public EsuException( string message, int code )
            : base( message ) {
            this.code = code;
        }

        /// <summary>
        /// Creates a new ESU exception with the given message and cause.
        /// The error code will be set to 0.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="cause">The error that caused the exception</param>
        public EsuException( string message, Exception cause )
            : base( message, cause ) {
            this.code = 0;
        }

        /// <summary>
        /// Creates a new ESU exception with the given message, cause, and
        /// error code.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="cause">The error that caused the exception</param>
        /// <param name="code">The error code from the ESU server</param>
        public EsuException( string message, Exception cause, int code )
            : base( message, cause ) {
            this.code = code;
        }

	
    }
}
