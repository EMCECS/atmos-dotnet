using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace EsuApiLib
{
    /// <summary>
    /// Encapsulates the data returned from the ReadObjectStream method.  Be sure to close this object instead of the stream
    /// to ensure the HTTP connection gets closed properly.
    /// </summary>
    public class ReadObjectStreamResponse
    {
        /// <summary>
        /// The stream containing the object's content
        /// </summary>
        public Stream Content { get; private set; }

        /// <summary>
        /// The content type (MIME Type) of the stream
        /// </summary>
        public string ContentType { get; private set; }

        /// <summary>
        /// The length of the stream.  May be -1 if the length is unknown or the response was chunked (usually when you request > 4MB of data).  If 
        /// you requested the entire object, you can check the 'size' attribute of the System Metadata.
        /// </summary>
        public long Length { get; private set; }

        /// <summary>
        /// User-defined metadata on the object
        /// </summary>
        public MetadataList Metadata { get; private set; }

        /// <summary>
        /// System-defined metadata on the object (size, ctime, mtime, etc)
        /// </summary>
        public Acl Acl { get; private set; }

        /// <summary>
        /// If an extent (slice) of the object was requested, it is copied here for reference.
        /// </summary>
        public Extent Extent { get; private set; }

        /// <summary>
        /// If a content checksum was stored for the object inside Atmos, it is returned here.
        /// </summary>
        public string ContentChecksum { get; private set; }

        private HttpWebResponse response;

        /// <summary>
        /// Constructs a new ReadObjectStreamResponse
        /// </summary>
        /// <param name="s">The object content stream</param>
        /// <param name="contentType">The MIME type of the stream</param>
        /// <param name="length">The length.  Set to -1 if unknown.</param>
        /// <param name="meta">The object metadata</param>
        /// <param name="acl">The object's ACL</param>
        /// <param name="extent">The extent that was read, or null</param>
        /// <param name="response">the HTTP response object to close the connection</param>
        /// <param name="checksum">The content checksum if the Atmos server had one stored</param>
        public ReadObjectStreamResponse(Stream s, string contentType, long length, MetadataList meta, Acl acl, Extent extent, HttpWebResponse response, string checksum)
        {
            this.Content = s;
            this.ContentType = contentType;
            this.Length = length;
            this.Metadata = meta;
            this.Acl = acl;
            this.response = response;
            this.ContentChecksum = checksum;
        }

        /// <summary>
        /// Closes the content stream and the underlying HTTP connection.  It is very important that you call
        /// this method when done with the stream otherwise the HTTP connection may stay open until the garbage
        /// collector closes it.
        /// </summary>
        public void Close()
        {
            try
            {
                Content.Close();
            }
            catch (Exception)
            {
                // Ignore
            }

            response.Close();
        }


    }
}
