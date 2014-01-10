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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EsuApiLib.Multipart
{
    public class MultipartException : Exception
    {
        public MultipartException() : base() {}
        public MultipartException(string message) : base(message) {}
        public MultipartException(string message, Exception innerException) : base(message, innerException) {}
    }

    public class MultipartPart
    {
        public string ContentType { get; private set; }
        public Extent ContentExtent { get; private set; }
        public byte[] Data { get; private set; }

        public MultipartPart(string contentType, Extent contentExtent, byte[] data)
        {
            this.ContentType = contentType;
            this.ContentExtent = contentExtent;
            this.Data = data;
        }
    }

    public class MultipartEntity : List<MultipartPart>
    {
        private static readonly Regex PATTERN_CONTENT_TYPE = new Regex( "^Content-Type: (.+)$" );
        private static readonly Regex PATTERN_CONTENT_RANGE = new Regex( "^Content-Range: bytes (\\d+)-(\\d+)/(\\d+)$" );

        /// <summary>
        /// Parses a multipart response body provided by an InputStream. Returns an instance of this class that represents
        /// the response. boundary may start with "--" or omit it.
        /// </summary>
        public static MultipartEntity FromStream( Stream s, string boundary ) {
            if ( boundary.StartsWith( "--" ) ) boundary = boundary.Substring( 2 );

            StreamReader reader = new StreamReader(s, Encoding.UTF8);
            List<MultipartPart> parts = new List<MultipartPart>();

            try {
                while ( true ) {

                    // first, we expect a boundary ( EOL + '--' + <boundary_string> + EOL )
                    if ( !"".Equals( reader.ReadLine() ) )
                        throw new MultipartException( "Parse error: expected EOL before boundary" );
                    String line = reader.ReadLine();

                    // two dashes after the boundary means EOS
                    if ( ("--" + boundary + "--").Equals( line ) ) break;

                    if ( !("--" + boundary).Equals( line ) ) throw new MultipartException(
                            "Parse error: expected [--" + boundary + "], instead got [" + line + "]" );

                    Match match;
                    String contentType = null;
                    int start = -1, end = 0, length = 0;
                    while ( !"".Equals( line = reader.ReadLine() ) ) {
                        match = PATTERN_CONTENT_TYPE.Match( line );
                        if ( match.Success ) {
                            contentType = match.Groups[1].Value;
                            continue;
                        }

                        match = PATTERN_CONTENT_RANGE.Match(line);
                        if (match.Success) {
                            start = Convert.ToInt32(match.Groups[1].Value);
                            end = Convert.ToInt32(match.Groups[2].Value);
                            length = end - start + 1;
                            // total = Integer.parseInt( matcher.group( 3 ) );
                            continue;
                        }

                        throw new MultipartException( "Unrecognized header line: " + line );
                    }

                    if ( contentType == null )
                        throw new MultipartException( "Parse error: No content-type specified in part" );

                    if ( start == -1 )
                        throw new MultipartException( "Parse error: No content-range specified in part" );

                    // then the data of the part
                    char[] data = new char[length];
                    int read, count = 0;
                    while ( count < length ) {
                        read = reader.Read( data, 0, length - count );
                        count += read;
                    }

                    parts.Add( new MultipartPart( contentType, new Extent( start, end - start + 1 ), Encoding.UTF8.GetBytes( data ) ) );
                }
            } finally {
                reader.Close();
            }

            return new MultipartEntity( parts );
        }

        public MultipartEntity( List<MultipartPart> parts ) : base(parts) {}

        /// <summary>
        /// Convenience method that aggregates the bytes of all parts into one contiguous byte array.
        /// </summary>
        /// <returns></returns>
        public byte[] AggregateBytes() {
            List<byte> allData = new List<byte>();
            foreach ( MultipartPart part in this ) {
                allData.AddRange(part.Data);
            }
            return allData.ToArray();
        }
    }
}
