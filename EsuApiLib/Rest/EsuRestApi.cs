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
using System.Collections.Specialized;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Reflection;
using System.Xml;
using System.Linq;
using System.Xml.Serialization;
using EsuApiLib.Multipart;

namespace EsuApiLib.Rest {
    /// <summary>
    /// Implements the REST version of the ESU API. This class uses 
    /// HttpWebRequest to perform object and metadata calls against 
    /// the ESU server. All of the methods that communicate with the 
    /// server are atomic and stateless so the object can be used 
    /// safely in a multithreaded environment. 
    /// </summary>
    public class EsuRestApi : EsuApi {
        private static readonly Regex OBJECTID_EXTRACTOR = new Regex( "/[0-9a-zA-Z]+/objects/([0-9a-f-]{44,})" );
        private static TraceSource log = new TraceSource("EsuRestApi");
        private static Encoding headerEncoder = Encoding.GetEncoding("iso-8859-1");


        private byte[] secret;
        private string context = "/rest";
        private int timeout = -1;
        private int readWriteTimeout = -1;
        private IWebProxy proxy;
        private int serverOffset = 0;
        private bool utf8Enabled = true;
        private Dictionary<string, string> customHeaders;

        /// <summary>
        /// Specifies a Proxy to use for connections to Atmos.
        /// </summary>
        public IWebProxy Proxy
        {
            get { return proxy; }
            set { proxy = value; }
        }

        /// <summary>
        /// Sets the timeout on the connection
        /// <remarks>Timeout is the number of milliseconds that a synchronous 
        /// request made waits for a response.  The default value is 100,000 milliseconds.</remarks>
        /// </summary>
        public int Timeout {
            get { return timeout; }
            set { timeout = value; }
        }

        /// <summary>
        /// Sets the read/write timeout on the connection.
        /// <remarks>Controls the timeout when reading and writing from streams.  Specifically,
        /// if you're using ReadObjectStream or Create/UpdateObjectFromStream, this controls
        /// the timeout when calling the Read/Write methods on the underlying stream.  The
        /// default value is 300,000 milliseconds.</remarks>
        /// </summary>
        public int ReadWriteTimeout
        {
            get { return readWriteTimeout; }
            set { readWriteTimeout = value; }
        }

        /// <summary>
        /// The context root of the web services on the server.  By default,
        /// the context root is "/rest".
        /// </summary>
        public string Context {
            get { return context; }
            set { context = value; }
        }

        private string host;

        /// <summary>
        /// The hostname or IP address of the ESU server.
        /// </summary>
        public string Host {
            get { return host; }
        }

        /// <summary>
        /// The port number for Atmos.  Generally, this is 80 for HTTP
        /// and 443 for HTTPS.
        /// </summary>
        protected int port;

        /// <summary>
        /// The port number of the ESU server.  Usually, this is 80 for
        /// HTTP and 443 for HTTPS.
        /// </summary>
        public int Port {
            get { return port; }
            set { port = value; }
        }

        private string uid;

        /// <summary>
        /// The user ID to used in requests
        /// </summary>
        public string Uid {
            get { return uid; }
        }

        /// <summary>
        /// The protocol used.  Generally, this is "http", or "https"
        /// </summary>
        protected string protocol;

        /// <summary>
        /// The protocol used.  Generally, this is "http", or "https".
        /// Use this to override the protocol, e.g. use https on port 10080.
        /// </summary>
        public string Protocol {
            get { return protocol; }
            set { protocol = value; }
        }

        /// <summary>
        /// The offset between local time and server time in seconds.  Set this value to
        /// adjust the outgoing timestamps to compensate for clock skew especially when
        /// NTP is not available.
        /// </summary>
        public int ServerOffset
        {
            get { return serverOffset; }
            set { serverOffset = value; }
        }

        /// <summary>
        /// Whether to enable UTF-8 character encoding in metadata (requires Atmos version 1.4.2+)
        /// </summary>
        public bool Utf8Enabled
        {
            get { return utf8Enabled; }
            set { utf8Enabled = value; }
        }

        /// <summary>
        /// Optional custom headers to use for each request (i.e. for a 3rd party authentication proxy)
        /// </summary>
        public Dictionary<string, string> CustomHeaders
        {
            get { return customHeaders; }
            set { customHeaders = value; }
        }

        /// <summary>
        /// Creates a new EsuRestApi object
        /// </summary>
        /// <param name="host">The hostname or IP address of the ESU server</param>
        /// <param name="port">The port on the server to communicate with. Generally this is 80 for HTTP and 443 for HTTPS.</param>
        /// <param name="uid">The username to use when connecting to the server</param>
        /// <param name="sharedSecret">The Base64 encoded shared secret to use to sign requests to the server.</param>
        public EsuRestApi( string host, int port, string uid, string sharedSecret ) {
            this.host = host;
            this.port = port;
            this.uid = uid;
            this.secret = Convert.FromBase64String( sharedSecret );
            if( port == 443 ) {
                protocol = "https";
            } else {
                protocol = "http";
            }
        }

        /// <summary>
        /// By default, when posting data .Net will not post a body on the first 
        /// request and tell the server it expects "100 Continue".  After getting
        /// "100 Continue" it then posts the data.  This is done to avoid sending
        /// the body when redirects or errors occur but can be detrimental to
        /// performance since more round trips are required.
        /// </summary>
        /// <param name="enabled">Set to true to enable continue behavior.  Set to false to disable it.</param>
        public void Set100Continue( bool enabled ) {
            ServicePointManager.FindServicePoint( buildUrl( "/objects" ) ).Expect100Continue = enabled;
        }

        #region EsuApi Members

        /// <summary>
        /// Creates a new object in the cloud.
        /// </summary>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <returns>Identifier of the newly created object.</returns>
        public ObjectId CreateObject(Acl acl, MetadataList metadata, byte[] data, string mimeType)
        {
            return CreateObjectFromSegment(acl, metadata,
                data == null ? new ArraySegment<byte>(new byte[0]) : new ArraySegment<byte>(data),
                mimeType, null);
        }

        /// <summary>
        /// Creates a new object in the cloud.
        /// </summary>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <param name="checksum">the checksum object to use to compute checksums.  If you're doing incremental updates after the create, include the same object in subsequent calls.  Can be null to omit checksums.</param>
        /// <returns>Identifier of the newly created object.</returns>
        public ObjectId CreateObject(Acl acl, MetadataList metadata, byte[] data, string mimeType, Checksum checksum)
        {
            return CreateObjectFromSegment(acl, metadata,
                data == null ? new ArraySegment<byte>(new byte[0]) : new ArraySegment<byte>(data),
                mimeType, checksum);
        }

        /// <summary>
        /// Creates a new object in the cloud using an ArraySegment.
        /// </summary>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <returns>Identifier of the newly created object.</returns>
        public ObjectId CreateObjectFromSegment(Acl acl, MetadataList metadata, ArraySegment<byte> data, string mimeType)
        {
            return CreateObjectFromSegment(acl, metadata, data, mimeType, null);
        }

        /// <summary>
        /// Calculates the time offset between the server and the local system in seconds.  You can set this
        /// value on the ServerOffset property to adjust outgoing timestamps to account for clock skew.
        /// Resolution is one second.
        /// </summary>
        /// <returns>The time offset between the server and the local system in seconds.</returns>
        public int CalculateServerOffset()
        {
            HttpWebResponse resp = null;
            try
            {
                string resource = context + "/";
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);
                resp = (HttpWebResponse)con.GetResponse();
            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }

            catch (WebException e)
            {
                //Don't fail if we get a WebException; we may be able to extract the date
                //header even from a failed request.
                if (e.Response != null)
                {
                    resp = (HttpWebResponse)e.Response;
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }

            if (resp != null)
            {
                string serverTime = resp.Headers[HttpResponseHeader.Date];
                resp.Close();
                if (serverTime == null)
                {
                    return 0;
                }

                // Parse into a GMT DateTime.
                DateTime dt = DateTime.Parse(serverTime).ToUniversalTime();
                DateTime here = DateTime.UtcNow;

                // Calculate offset
                TimeSpan offset = dt.Subtract(here);
                return (int)offset.TotalSeconds;
            }
            else
            {
                resp.Close();
                return 0;
            }
        }


        /// <summary>
        /// Creates a new object in the cloud using an ArraySegment.
        /// </summary>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <param name="checksum">the checksum object to use to compute checksums.  If you're doing incremental updates after the create, include the same object in subsequent calls.  Can be null to omit checksums.</param>
        /// <returns>Identifier of the newly created object.</returns>
        public ObjectId CreateObjectFromSegment( Acl acl, MetadataList metadata, ArraySegment<byte> data, string mimeType, Checksum checksum ) {
            HttpWebResponse resp = null;
            try {
                string resource = context + "/objects";
                Uri u = buildUrl( resource );
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                // Figure out the mimetype
                if( mimeType == null ) {
                    mimeType = "application/octet-stream";
                }

                headers.Add( "Content-Type", mimeType );
                headers.Add( "x-emc-uid", uid );

                // Process metadata
                if( metadata != null ) {
                    processMetadata( metadata, headers );
                }

                // Add acl
                if( acl != null ) {
                    processAcl( acl, headers );
                }

                // Add date
                addDateHeader(headers);

                // Checksum if required
                if (checksum != null)
                {
                    checksum.Update(data);
                    //Checksum ckcopy = checksum.Clone();
                    headers.Add("x-emc-wschecksum", checksum.ToString() );
                }

                // Sign request
                signRequest( con, "POST", resource, headers );

                // post data
                Stream s = null;
                try {
                    con.ContentLength = data.Count;
                    con.SendChunked = false;
                    s = con.GetRequestStream();
                    s.Write( data.Array, data.Offset, data.Count );
                    s.Close();
                } catch( IOException e ) {
                    s.Close();
                    throw new EsuException( "Error posting data", e );
                }

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

                // The new object ID is returned in the location response header
                string location = resp.Headers["location"];

                // Parse the value out of the URL

                MatchCollection m = OBJECTID_EXTRACTOR.Matches( location );
                if( m.Count > 0 ) {
                    string id = m[0].Groups[1].Value;
                    log.TraceEvent(TraceEventType.Verbose, 0,  "Id: " + id );
                    return new ObjectId( id );
                } else {
                    throw new EsuException( "Could not find ObjectId in " + location );
                }
            } catch( UriFormatException e ) {
                throw new EsuException( "Invalid URL", e );
            } catch( IOException e ) {
                throw new EsuException( "Error connecting to server", e );
            } catch( WebException e ) {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            } finally {
                if( resp != null ) {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a new object in the cloud reading the content a Stream.
        /// </summary>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  Note that we will read only 'streamLength' bytes from the stream and do not close it</param>
        /// <param name="streamLength">The number of bytes to read from the stream.  Must be &lt;= the actual number of bytes in the stream.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <param name="checksum">the checksum object to use to compute checksums.  If you're doing incremental updates after the create, include the same object in subsequent calls.  Can be null to omit checksums.</param>
        /// <returns>Identifier of the newly created object.</returns>
        public ObjectId CreateObjectFromStream(Acl acl, MetadataList metadata, Stream data, long streamLength, string mimeType, Checksum checksum)
        {
            HttpWebResponse resp = null;
            try
            {
                string resource = context + "/objects";
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                // Figure out the mimetype
                if (mimeType == null)
                {
                    mimeType = "application/octet-stream";
                }

                headers.Add("Content-Type", mimeType);
                headers.Add("x-emc-uid", uid);

                // Process metadata
                if (metadata != null)
                {
                    processMetadata(metadata, headers);
                }

                // Add acl
                if (acl != null)
                {
                    processAcl(acl, headers);
                }

                // Add date
                addDateHeader(headers);

                // Checksum if required
                if (checksum != null)
                {
                    if (!data.CanSeek)
                    {
                        throw new EsuException("Cannot checksum a stream that does not support seeking");
                    }
                    long current = data.Position;
                    byte[] buffer = new byte[64 * 1024];
                    for(long i=0; i<streamLength; i+=buffer.Length) {
                        if(i+buffer.Length>streamLength) {
                            int bytesToRead = (int)(streamLength-i);
                            int count = data.Read(buffer, 0, bytesToRead);
                            checksum.Update(new ArraySegment<byte>(buffer, 0, count));
                        } else {
                            int count = data.Read(buffer, 0, buffer.Length);
                            checksum.Update(new ArraySegment<byte>(buffer, 0, count));
                        }
                    }
                    data.Seek(current, SeekOrigin.Begin);

                    //Checksum ckcopy = checksum.Clone();
                    headers.Add("x-emc-wschecksum", checksum.ToString());
                }

                // Sign request
                signRequest(con, "POST", resource, headers);

                writeRequestBodyFromStream(con, data, streamLength);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }

                // The new object ID is returned in the location response header
                string location = resp.Headers["location"];

                // Parse the value out of the URL

                MatchCollection m = OBJECTID_EXTRACTOR.Matches(location);
                if (m.Count > 0)
                {
                    string id = m[0].Groups[1].Value;
                    log.TraceEvent(TraceEventType.Verbose, 0, "Id: " + id);
                    return new ObjectId(id);
                }
                else
                {
                    throw new EsuException("Could not find ObjectId in " + location);
                }
            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a new object in the cloud reading the content a Stream.
        /// </summary>
        /// <param name="path">The path to create the new object on</param>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  Note that we only read 'streamLength' bytes from the stream and do not close the stream.</param>
        /// <param name="streamLength">The number of bytes to read from the stream.  Must be &lt;= the actual stream length.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <param name="checksum">the checksum object to use to compute checksums.  If you're doing incremental updates after the create, include the same object in subsequent calls.  Can be null to omit checksums.</param>
        /// <returns>Identifier of the newly created object.</returns>
        public ObjectId CreateObjectFromStreamOnPath(ObjectPath path, Acl acl, MetadataList metadata, Stream data, long streamLength, string mimeType, Checksum checksum)
        {
            HttpWebResponse resp = null;
            try
            {
                string resource = getResourcePath(context, path);
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                // Figure out the mimetype
                if (mimeType == null)
                {
                    mimeType = "application/octet-stream";
                }

                headers.Add("Content-Type", mimeType);
                headers.Add("x-emc-uid", uid);

                // Process metadata
                if (metadata != null)
                {
                    processMetadata(metadata, headers);
                }

                // Add acl
                if (acl != null)
                {
                    processAcl(acl, headers);
                }

                // Add date
                addDateHeader(headers);

                // Checksum if required
                if (checksum != null)
                {
                    if (!data.CanSeek)
                    {
                        throw new EsuException("Cannot checksum a stream that does not support seeking");
                    }
                    long current = data.Position;
                    byte[] buffer = new byte[64 * 1024];
                    for (long i = 0; i < streamLength; i += buffer.Length)
                    {
                        if (i + buffer.Length > streamLength)
                        {
                            int bytesToRead = (int)(streamLength - i);
                            int count = data.Read(buffer, 0, bytesToRead);
                            checksum.Update(new ArraySegment<byte>(buffer, 0, count));
                        }
                        else
                        {
                            int count = data.Read(buffer, 0, buffer.Length);
                            checksum.Update(new ArraySegment<byte>(buffer, 0, count));
                        }
                    }
                    data.Seek(current, SeekOrigin.Begin);

                    //Checksum ckcopy = checksum.Clone();
                    headers.Add("x-emc-wschecksum", checksum.ToString());
                }

                // Sign request
                signRequest(con, "POST", resource, headers);

                writeRequestBodyFromStream(con, data, streamLength);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }

                // The new object ID is returned in the location response header
                string location = resp.Headers["location"];

                // Parse the value out of the URL

                MatchCollection m = OBJECTID_EXTRACTOR.Matches(location);
                if (m.Count > 0)
                {
                    string id = m[0].Groups[1].Value;
                    log.TraceEvent(TraceEventType.Verbose, 0, "Id: " + id);
                    return new ObjectId(id);
                }
                else
                {
                    throw new EsuException("Could not find ObjectId in " + location);
                }
            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }
            return null;
        }



        /// <summary>
        /// Creates a new object in the cloud on the
        /// given path.
        /// </summary>
        /// <param name="path">the path to create the object on.</param>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content or a directory.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <returns>the ObjectId of the newly-created object for references by ID.</returns>
        public ObjectId CreateObjectOnPath(ObjectPath path, Acl acl,
                MetadataList metadata,
                byte[] data, String mimeType)
        {
            return CreateObjectFromSegmentOnPath(path, acl, metadata,
                data == null ? new ArraySegment<byte>(new byte[0]) : new ArraySegment<byte>(data),
                mimeType, null);
        }

        /// <summary>
        /// Creates a new object in the cloud on the
        /// given path.
        /// </summary>
        /// <param name="path">the path to create the object on.</param>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content or a directory.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <param name="checksum">the checksum object to use to compute checksums.  If you're doing incremental updates after the create, include the same object in subsequent calls.  Can be null to omit checksums.</param>
        /// <returns>the ObjectId of the newly-created object for references by ID.</returns>
        public ObjectId CreateObjectOnPath(ObjectPath path, Acl acl,
                MetadataList metadata,
                byte[] data, String mimeType, Checksum checksum)
        {
            return CreateObjectFromSegmentOnPath(path, acl, metadata,
                data == null ? new ArraySegment<byte>(new byte[0]) : new ArraySegment<byte>(data),
                mimeType, checksum);
        }

        /// <summary>
        /// Creates a new object in the cloud using a BufferSegment on the
        /// given path.
        /// </summary>
        /// <param name="path">the path to create the object on.</param>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content or a directory.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <returns>the ObjectId of the newly-created object for references by ID.</returns>
        public ObjectId CreateObjectFromSegmentOnPath(ObjectPath path,
                Acl acl, MetadataList metadata,
                ArraySegment<byte> data, String mimeType)
        {
            return CreateObjectFromSegmentOnPath(path, acl, metadata, data, mimeType, null);
        }

        /// <summary>
        /// Creates a new object in the cloud using a BufferSegment on the
        /// given path.
        /// </summary>
        /// <param name="path">the path to create the object on.</param>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content or a directory.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <param name="checksum">the checksum object to use to compute checksums.  If you're doing incremental updates after the create, include the same object in subsequent calls.  Can be null to omit checksums.</param>
        /// <returns>the ObjectId of the newly-created object for references by ID.</returns>
        public ObjectId CreateObjectFromSegmentOnPath(ObjectPath path,
                Acl acl, MetadataList metadata,
                ArraySegment<byte> data, String mimeType, Checksum checksum) {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath( context, path );
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                // Figure out the mimetype
                if (mimeType == null) {
                    mimeType = "application/octet-stream";
                }

                headers.Add("Content-Type", mimeType);
                headers.Add("x-emc-uid", uid);

                // Process metadata
                if (metadata != null) {
                    processMetadata(metadata, headers);
                }

                // Add acl
                if (acl != null) {
                    processAcl(acl, headers);
                }

                // Add date
                addDateHeader(headers);

                // Checksum if required
                if (checksum != null)
                {
                    checksum.Update(data);
                    headers.Add("x-emc-wschecksum", checksum.ToString());
                }

                // Sign request
                signRequest(con, "POST", resource, headers);

                // post data
                Stream s = null;
                try {
                    con.ContentLength = data.Count;
                    con.SendChunked = false;
                    s = con.GetRequestStream();
                    s.Write(data.Array, data.Offset, data.Count);
                    s.Close();
                } catch (IOException e) {
                    s.Close();
                    throw new EsuException("Error posting data", e);
                }

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299) {
                    handleError(resp);
                }

                // The new object ID is returned in the location response header
                string location = resp.Headers["location"];

                // Parse the value out of the URL

                MatchCollection m = OBJECTID_EXTRACTOR.Matches(location);
                if (m.Count > 0) {
                    string id = m[0].Groups[1].Value;
                    log.TraceEvent(TraceEventType.Verbose, 0, "Id: " + id);
                    return new ObjectId(id);
                } else {
                    throw new EsuException("Could not find ObjectId in " + location);
                }
            } catch (UriFormatException e) {
                throw new EsuException("Invalid URL", e);
            } catch (IOException e) {
                throw new EsuException("Error connecting to server", e);
            } catch (WebException e) {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null) {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a new object in the cloud with the given key (and key-pool).
        /// </summary>
        /// <param name="key">the key-pool and key to use for the new object.</param>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content or a directory.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <returns>the ObjectId of the newly-created object for references by ID.</returns>
        public ObjectId CreateObjectWithKey(ObjectKey key, Acl acl, MetadataList metadata, byte[] data,
                String mimeType)
        {
            return CreateObjectFromSegmentWithKey(key, acl, metadata, new ArraySegment<byte>(data, 0, data.Length), mimeType);
        }

        /// <summary>
        /// Creates a new object in the cloud with the given key (and key-pool).
        /// </summary>
        /// <param name="key">the key-pool and key to use for the new object.</param>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content or a directory.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <param name="checksum">the checksum object to use to compute checksums.  If you're doing incremental updates after the create, include the same object in subsequent calls.  Can be null to omit checksums.</param>
        /// <returns>the ObjectId of the newly-created object for references by ID.</returns>
        public ObjectId CreateObjectWithKey(ObjectKey key, Acl acl, MetadataList metadata, byte[] data,
                String mimeType, Checksum checksum)
        {
            return CreateObjectFromSegmentWithKey(key, acl, metadata, new ArraySegment<byte>(data, 0, data.Length), mimeType, checksum);
        }

        /// <summary>
        /// Creates a new object in the cloud with the given key (and key-pool).
        /// </summary>
        /// <param name="key">the key-pool and key to use for the new object.</param>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content or a directory.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <returns>the ObjectId of the newly-created object for references by ID.</returns>
        public ObjectId CreateObjectFromSegmentWithKey(ObjectKey key, Acl acl, MetadataList metadata,
                ArraySegment<byte> data, String mimeType)
        {
            return CreateObjectFromSegmentWithKey(key, acl, metadata, data, mimeType, null);
        }

        /// <summary>
        /// Creates a new object in the cloud with the given key (and key-pool).
        /// </summary>
        /// <param name="key">the key-pool and key to use for the new object.</param>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content or a directory.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <param name="checksum">the checksum object to use to compute checksums.  If you're doing incremental updates after the create, include the same object in subsequent calls.  Can be null to omit checksums.</param>
        /// <returns>the ObjectId of the newly-created object for references by ID.</returns>
        public ObjectId CreateObjectFromSegmentWithKey(ObjectKey key, Acl acl, MetadataList metadata, ArraySegment<byte> data,
                String mimeType, Checksum checksum)
        {
            HttpWebResponse resp = null;
            try
            {
                string resource = getResourcePath(context, key);
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                // Figure out the mimetype
                if (mimeType == null)
                {
                    mimeType = "application/octet-stream";
                }

                headers.Add("Content-Type", mimeType);
                headers.Add("x-emc-uid", uid);
                headers.Add("x-emc-pool", key.pool);

                // Process metadata
                if (metadata != null)
                {
                    processMetadata(metadata, headers);
                }

                // Add acl
                if (acl != null)
                {
                    processAcl(acl, headers);
                }

                // Add date
                addDateHeader(headers);

                // Checksum if required
                if (checksum != null)
                {
                    checksum.Update(data);
                    headers.Add("x-emc-wschecksum", checksum.ToString());
                }

                // Sign request
                signRequest(con, "POST", resource, headers);

                // post data
                Stream s = null;
                try
                {
                    con.ContentLength = data.Count;
                    con.SendChunked = false;
                    s = con.GetRequestStream();
                    s.Write(data.Array, data.Offset, data.Count);
                    s.Close();
                }
                catch (IOException e)
                {
                    s.Close();
                    throw new EsuException("Error posting data", e);
                }

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }

                // The new object ID is returned in the location response header
                string location = resp.Headers["location"];

                // Parse the value out of the URL

                MatchCollection m = OBJECTID_EXTRACTOR.Matches(location);
                if (m.Count > 0)
                {
                    string id = m[0].Groups[1].Value;
                    log.TraceEvent(TraceEventType.Verbose, 0, "Id: " + id);
                    return new ObjectId(id);
                }
                else
                {
                    throw new EsuException("Could not find ObjectId in " + location);
                }
            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a new object in the cloud with the given key (and key-pool).
        /// </summary>
        /// <param name="key">the key-pool and key to use for the new object.</param>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  Note that we will read only 'streamLength' bytes from the stream and do not close it</param>
        /// <param name="streamLength">The number of bytes to read from the stream.  Must be &lt;= the actual number of bytes in the stream.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <param name="checksum">the checksum object to use to compute checksums.  If you're doing incremental updates after the create, include the same object in subsequent calls.  Can be null to omit checksums.</param>
        /// <returns>the ObjectId of the newly-created object for references by ID.</returns>
        public ObjectId CreateObjectFromStreamWithKey(ObjectKey key, Acl acl, MetadataList metadata, Stream data, long streamLength, string mimeType, Checksum checksum)
        {
            HttpWebResponse resp = null;
            try
            {
                string resource = getResourcePath(context, key);
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                // Figure out the mimetype
                if (mimeType == null)
                {
                    mimeType = "application/octet-stream";
                }

                headers.Add("Content-Type", mimeType);
                headers.Add("x-emc-uid", uid);
                headers.Add("x-emc-pool", key.pool);

                // Process metadata
                if (metadata != null)
                {
                    processMetadata(metadata, headers);
                }

                // Add acl
                if (acl != null)
                {
                    processAcl(acl, headers);
                }

                // Add date
                addDateHeader(headers);

                // Checksum if required
                if (checksum != null)
                {
                    if (!data.CanSeek)
                    {
                        throw new EsuException("Cannot checksum a stream that does not support seeking");
                    }
                    long current = data.Position;
                    byte[] buffer = new byte[64 * 1024];
                    for (long i = 0; i < streamLength; i += buffer.Length)
                    {
                        if (i + buffer.Length > streamLength)
                        {
                            int bytesToRead = (int)(streamLength - i);
                            int count = data.Read(buffer, 0, bytesToRead);
                            checksum.Update(new ArraySegment<byte>(buffer, 0, count));
                        }
                        else
                        {
                            int count = data.Read(buffer, 0, buffer.Length);
                            checksum.Update(new ArraySegment<byte>(buffer, 0, count));
                        }
                    }
                    data.Seek(current, SeekOrigin.Begin);

                    //Checksum ckcopy = checksum.Clone();
                    headers.Add("x-emc-wschecksum", checksum.ToString());
                }

                // Sign request
                signRequest(con, "POST", resource, headers);

                writeRequestBodyFromStream(con, data, streamLength);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }

                // The new object ID is returned in the location response header
                string location = resp.Headers["location"];

                // Parse the value out of the URL

                MatchCollection m = OBJECTID_EXTRACTOR.Matches(location);
                if (m.Count > 0)
                {
                    string id = m[0].Groups[1].Value;
                    log.TraceEvent(TraceEventType.Verbose, 0, "Id: " + id);
                    return new ObjectId(id);
                }
                else
                {
                    throw new EsuException("Could not find ObjectId in " + location);
                }
            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Updates an object in the cloud.
        /// </summary>
        /// <param name="id">The ID of the object to update</param>
        /// <param name="acl">Access control list for the new object. Optional, set to NULL to leave the ACL unchanged.</param>
        /// <param name="metadata">Metadata list for the new object.  Optional, set to NULL for no changes to the metadata.</param>
        /// <param name="extent">portion of the object to update.  May be null to indicate the whole object is to be replaced.  If not null, the extent size must match the data size.</param>
        /// <param name="data">The new contents of the object.  May be appended to later. Optional, set to null for no content changes.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        public void UpdateObject(Identifier id, Acl acl, MetadataList metadata, Extent extent, byte[] data, string mimeType)
        {
            ArraySegment<byte> seg = new ArraySegment<byte>(data == null ? new byte[0] : data);
            UpdateObjectFromSegment(id, acl, metadata, extent, seg, mimeType, null);
        }

        /// <summary>
        /// Updates an object in the cloud.
        /// </summary>
        /// <param name="id">The ID of the object to update</param>
        /// <param name="acl">Access control list for the new object. Optional, set to NULL to leave the ACL unchanged.</param>
        /// <param name="metadata">Metadata list for the new object.  Optional, set to NULL for no changes to the metadata.</param>
        /// <param name="extent">portion of the object to update.  May be null to indicate the whole object is to be replaced.  If not null, the extent size must match the data size.</param>
        /// <param name="data">The new contents of the object.  May be appended to later. Optional, set to null for no content changes.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <param name="checksum">the checksum object to use to compute checksums.  If you're doing incremental updates after the create, include the same object in subsequent calls.  Can be null to omit checksums.</param>
        public void UpdateObject(Identifier id, Acl acl, MetadataList metadata, Extent extent, byte[] data, string mimeType, Checksum checksum)
        {
            ArraySegment<byte> seg = new ArraySegment<byte>(data == null ? new byte[0] : data);
            UpdateObjectFromSegment(id, acl, metadata, extent, seg, mimeType, checksum);
        }

        /// <summary>
        /// Updates an object in the cloud.
        /// </summary>
        /// <param name="id">The ID of the object to update</param>
        /// <param name="acl">Access control list for the new object. Optional, set to NULL to leave the ACL unchanged.</param>
        /// <param name="metadata">Metadata list for the new object.  Optional, set to NULL for no changes to the metadata.</param>
        /// <param name="extent">portion of the object to update.  May be null to indicate the whole object is to be replaced.  If not null, the extent size must match the data size.</param>
        /// <param name="data">The new contents of the object.  May be appended to later. Optional, set to null for no content changes.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        public void UpdateObjectFromSegment(Identifier id, Acl acl, MetadataList metadata, Extent extent, ArraySegment<byte> data, string mimeType)
        {
            UpdateObjectFromSegment(id, acl, metadata, extent, data, mimeType, null);
        }

        /// <summary>
        /// Updates an object in the cloud.
        /// </summary>
        /// <param name="id">The ID of the object to update</param>
        /// <param name="acl">Access control list for the new object. Optional, set to NULL to leave the ACL unchanged.</param>
        /// <param name="metadata">Metadata list for the new object.  Optional, set to NULL for no changes to the metadata.</param>
        /// <param name="extent">portion of the object to update.  May be null to indicate the whole object is to be replaced.  If not null, the extent size must match the data size.</param>
        /// <param name="data">The new contents of the object.  May be appended to later. Optional, set to null for no content changes.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <param name="checksum">the checksum object to use to compute checksums.  If you're doing incremental updates after the create, include the same object in subsequent calls.  Can be null to omit checksums.</param>
        public void UpdateObjectFromSegment(Identifier id, Acl acl, MetadataList metadata, Extent extent, ArraySegment<byte> data, string mimeType, Checksum checksum)
        {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath(context, id);
                Uri u = buildUrl( resource );
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                // Figure out the mimetype
                if( mimeType == null ) {
                    mimeType = "application/octet-stream";
                }

                headers.Add( "Content-Type", mimeType );
                headers.Add( "x-emc-uid", uid );
                if (id is ObjectKey) {
                    headers.Add( "x-emc-pool", (id as ObjectKey).pool );
                }

                //Add extent if needed
                if( extent != null && !extent.Equals( Extent.ALL_CONTENT ) ) {
                    long end = extent.Offset + (extent.Size - 1);
                    headers.Add( "Range", "Bytes=" + extent.Offset + "-" + end );
                }

                // Process metadata
                if( metadata != null ) {
                    processMetadata( metadata, headers );
                }

                // Add acl
                if( acl != null ) {
                    processAcl( acl, headers );
                }

                // Add date
                addDateHeader(headers);

                // Checksum if required
                if (checksum != null)
                {
                    checksum.Update(data);
                    headers.Add("x-emc-wschecksum", checksum.ToString());
                }

                // Sign request
                signRequest( con, "PUT", resource, headers );

                // post data
                Stream s = null;
                try {
                    s = con.GetRequestStream();
                    s.Write( data.Array, data.Offset, data.Count );
                    s.Close();
                } catch( IOException e ) {
                    s.Close();
                    throw new EsuException( "Error posting data", e );
                }

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

            } catch( UriFormatException e ) {
                throw new EsuException( "Invalid URL", e );
            } catch( IOException e ) {
                throw new EsuException( "Error connecting to server", e );
            } catch( WebException e ) {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if( resp != null ) {
                    resp.Close();
                }
            }
        }

        /// <summary>
        /// Updates an object in the cloud.
        /// </summary>
        /// <param name="id">The ID of the object to update</param>
        /// <param name="acl">Access control list for the new object. Optional, set to NULL to leave the ACL unchanged.</param>
        /// <param name="metadata">Metadata list for the new object.  Optional, set to NULL for no changes to the metadata.</param>
        /// <param name="extent">portion of the object to update.  May be null to indicate the whole object is to be replaced.  If not null, the extent size must match the data size.</param>
        /// <param name="data">The initial contents of the object.  Note that we only read 'streamLength' bytes from the stream and do not close the stream.</param>
        /// <param name="streamLength">The number of bytes to read from the stream.  Must be &lt;= the actual stream length.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <param name="checksum">the checksum object to use to compute checksums.  If you're doing incremental updates after the create, include the same object in subsequent calls.  Can be null to omit checksums.</param>
        public void UpdateObjectFromStream(Identifier id, Acl acl, MetadataList metadata, Extent extent, Stream data, long streamLength, string mimeType, Checksum checksum)
        {
            HttpWebResponse resp = null;
            try
            {
                string resource = getResourcePath(context, id);
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                // Figure out the mimetype
                if (mimeType == null)
                {
                    mimeType = "application/octet-stream";
                }

                headers.Add("Content-Type", mimeType);
                headers.Add("x-emc-uid", uid);
                if (id is ObjectKey) {
                    headers.Add("x-emc-pool", (id as ObjectKey).pool);
                }

                //Add extent if needed
                if (extent != null && !extent.Equals(Extent.ALL_CONTENT))
                {
                    long end = extent.Offset + (extent.Size - 1);
                    headers.Add("Range", "Bytes=" + extent.Offset + "-" + end);
                }

                // Process metadata
                if (metadata != null)
                {
                    processMetadata(metadata, headers);
                }

                // Add acl
                if (acl != null)
                {
                    processAcl(acl, headers);
                }

                // Add date
                addDateHeader(headers);

                // Checksum if required
                if (checksum != null)
                {
                    if (!data.CanSeek)
                    {
                        throw new EsuException("Cannot checksum a stream that does not support seeking");
                    }
                    long current = data.Position;
                    byte[] buffer = new byte[64 * 1024];
                    for (long i = 0; i < streamLength; i += buffer.Length)
                    {
                        if (i + buffer.Length > streamLength)
                        {
                            int bytesToRead = (int)(streamLength - i);
                            int count = data.Read(buffer, 0, bytesToRead);
                            checksum.Update(new ArraySegment<byte>(buffer, 0, count));
                        }
                        else
                        {
                            int count = data.Read(buffer, 0, buffer.Length);
                            checksum.Update(new ArraySegment<byte>(buffer, 0, count));
                        }
                    }
                    data.Seek(current, SeekOrigin.Begin);

                    //Checksum ckcopy = checksum.Clone();
                    headers.Add("x-emc-wschecksum", checksum.ToString());
                }


                // Sign request
                signRequest(con, "PUT", resource, headers);

                // post data
                writeRequestBodyFromStream(con, data, streamLength);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }

            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }
        }



        /// <summary>
        /// Fetches the user metadata for the object.
        /// </summary>
        /// <param name="id">the identifier of the object whose user metadata to fetch.</param>
        /// <param name="tags">A list of user metadata tags to fetch.  Optional.  If null, all user metadata will be fetched.</param>
        /// <returns>The list of user metadata for the object.</returns>
        public MetadataList GetUserMetadata( Identifier id, MetadataTags tags ) {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath(context, id) + "?metadata/user";
                Uri u = buildUrl( resource );
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );
                if (utf8Enabled) headers.Add("x-emc-utf8", "true");
                if (id is ObjectKey) {
                    headers.Add("x-emc-pool", (id as ObjectKey).pool);
                }

                // Add tags if needed
                if( tags != null ) {
                    processTags( tags, headers );
                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest( con, "GET", resource, headers );

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

                // Parse return headers.  Regular metadata is in x-emc-meta and
                // listable metadata is in x-emc-listable-meta
                MetadataList meta = new MetadataList();
                readMetadata( meta, resp.Headers["x-emc-meta"], false );
                readMetadata( meta, resp.Headers["x-emc-listable-meta"], true );

                return meta;

            } catch( UriFormatException e ) {
                throw new EsuException( "Invalid URL", e );
            } catch( IOException e ) {
                throw new EsuException( "Error connecting to server", e );
            } catch( WebException e ) {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if( resp != null ) {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Fetches the system metadata for the object.
        /// </summary>
        /// <param name="id">the identifier of the object whose system metadata to fetch.</param>
        /// <param name="tags">A list of system metadata tags to fetch.  Optional.  If null, all metadata will be fetched.</param>
        /// <returns>The list of system metadata for the object.</returns>
        public MetadataList GetSystemMetadata( Identifier id, MetadataTags tags ) {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath(context, id) + "?metadata/system";
                Uri u = buildUrl( resource );
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );
                if (id is ObjectKey) {
                    headers.Add("x-emc-pool", (id as ObjectKey).pool);
                }

                // Add tags if needed
                if( tags != null ) {
                    processTags( tags, headers );
                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest( con, "GET", resource, headers );

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

                // Parse return headers.  Regular metadata is in x-emc-meta and
                // listable metadata is in x-emc-listable-meta
                MetadataList meta = new MetadataList();
                readMetadata( meta, resp.Headers["x-emc-meta"], false );
                readMetadata( meta, resp.Headers["x-emc-listable-meta"], true );

                return meta;

            } catch( UriFormatException e ) {
                throw new EsuException( "Invalid URL", e );
            } catch( IOException e ) {
                throw new EsuException( "Error connecting to server", e );
            } catch( WebException e ) {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if( resp != null ) {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Fetches object content as a stream
        /// </summary>
        /// <param name="id">the identifier of the object whose content to read.</param>
        /// <param name="extent">the portion of the object data to read.  Optional.  If null, the entire object will be read.</param>
        /// <returns></returns>
        public ReadObjectStreamResponse ReadObjectStream(Identifier id, Extent extent)
        {
            HttpWebResponse resp = null;
            try
            {
                string resource = getResourcePath(context, id);
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add("x-emc-uid", uid);
                if (id is ObjectKey) {
                    headers.Add("x-emc-pool", (id as ObjectKey).pool);
                }

                //Add extent if needed
                if (extent != null && !extent.Equals(Extent.ALL_CONTENT))
                {
                    long end = extent.Offset + (extent.Size - 1);
                    headers.Add("Range", "Bytes=" + extent.Offset + "-" + end);
                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest(con, "GET", resource, headers);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }

                string contentChecksum = resp.Headers["x-emc-wschecksum"];

                // Parse return headers.  Regular metadata is in x-emc-meta and
                // listable metadata is in x-emc-listable-meta
                MetadataList meta = new MetadataList();
                readMetadata(meta, resp.Headers["x-emc-meta"], false);
                readMetadata(meta, resp.Headers["x-emc-listable-meta"], true);

                // Parse return headers.  User grants are in x-emc-useracl and
                // group grants are in x-emc-groupacl
                Acl acl = new Acl();
                readAcl(acl, resp.Headers["x-emc-useracl"], Grantee.GRANTEE_TYPE.USER);
                readAcl(acl, resp.Headers["x-emc-groupacl"], Grantee.GRANTEE_TYPE.GROUP);

                long streamLength = resp.ContentLength;
                
                return new ReadObjectStreamResponse(resp.GetResponseStream(), resp.ContentType,
                    streamLength, meta, acl, extent, resp, contentChecksum);
            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                if (resp != null)
                {
                    resp.Close();
                }
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                    if (resp != null)
                    {
                        resp.Close();
                    }
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }

            return null;
        }

        /// <summary>
        /// Reads an object's content.
        /// </summary>
        /// <param name="id">the identifier of the object whose content to read.</param>
        /// <param name="extent">the portion of the object data to read.  Optional.  If null, the entire object will be read.</param>
        /// <param name="buffer">the buffer to use to read the extent.  Must be large enough to read the response or an error will be thrown.  If null, a buffer will be allocated to hold the response data.  If you pass a buffer that is larger than the extent, only extent.getSize() bytes will be valid.</param>
        /// <returns>A byte array containing the requested content.</returns>
        public byte[] ReadObject(Identifier id, Extent extent, byte[] buffer)
        {
            return ReadObject(id, extent, buffer, null);
        }

        /// <summary>
        /// Reads an object's content.
        /// </summary>
        /// <param name="id">the identifier of the object whose content to read.</param>
        /// <param name="extent">the portion of the object data to read.  Optional.  If null, the entire object will be read.</param>
        /// <param name="buffer">the buffer to use to read the extent.  Must be large enough to read the response or an error will be thrown.  If null, a buffer will be allocated to hold the response data.  If you pass a buffer that is larger than the extent, only extent.getSize() bytes will be valid.</param>
        /// <param name="checksum">checksum if not null, the given checksum object will be used
        /// to verify checksums during the read operation.  Note that only erasure coded objects 
        /// will return checksums *and* if you're reading the object in chunks, you'll have to 
        /// read the data back sequentially to keep the checksum consistent.  If the read operation 
        /// does not return a checksum from the server, the checksum operation will be skipped.</param>
        /// <returns>A byte array containing the requested content.</returns>
        public byte[] ReadObject( Identifier id, Extent extent, byte[] buffer, Checksum checksum ) {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath(context, id);
                Uri u = buildUrl( resource );
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );
                if (id is ObjectKey) {
                    headers.Add("x-emc-pool", (id as ObjectKey).pool);
                }

                //Add extent if needed
                if( extent != null && !extent.Equals( Extent.ALL_CONTENT ) ) {
                    long end = extent.Offset + (extent.Size - 1);
                    headers.Add( "Range", "Bytes=" + extent.Offset + "-" + end );
                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest( con, "GET", resource, headers );

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

                byte[] responseBuffer = readResponse( resp, buffer, extent );

                if (checksum != null && resp.Headers["x-emc-wschecksum"] != null)
                {
                    // Update checksum
                    int contentLength = (int)resp.ContentLength;
                    checksum.ExpectedValue = resp.Headers["x-emc-wschecksum"];
                    if (contentLength == -1 && extent == null)
                    {
                        // Use buffer size
                        checksum.Update(new ArraySegment<byte>(responseBuffer, 0, responseBuffer.Length));
                    }
                    else if (contentLength == -1 && extent != null)
                    {
                        // Use extent size
                        checksum.Update(new ArraySegment<byte>(responseBuffer, 0, (int)extent.Size));
                    }
                    else
                    {
                        checksum.Update(new ArraySegment<byte>(responseBuffer, 0, contentLength));
                    }
                }

                resp.Close();
                return responseBuffer;

            } catch( UriFormatException e ) {
                throw new EsuException( "Invalid URL", e );
            } catch( IOException e ) {
                throw new EsuException( "Error connecting to server", e );
            } catch( WebException e ) {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if( resp != null ) {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Reads content from multiple extents within an object using a single call.
        /// </summary>
        /// <param name="id">the identifier of the object whose content to read.</param>
        /// <param name="extents">the extents of the object data to read.</param>
        /// <returns>A MultipartEntity, which is also a List&lt;MultipartPart&gt;</MultipartPart>,
        /// but provides a method to aggregate data from the parts into a single byte array.</returns>
        public MultipartEntity ReadObjectExtents(Identifier id, params Extent[] extents)
        {
            if (extents == null || !extents.Any()) throw new EsuException("You must specify extents for this call");
            HttpWebResponse resp = null;
            try
            {
                string resource = getResourcePath(context, id);
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add("x-emc-uid", uid);
                if (id is ObjectKey)
                {
                    headers.Add("x-emc-pool", (id as ObjectKey).pool);
                }

                //Add extents
                string range = "Bytes=";
                foreach (Extent extent in extents)
                {
                    range += extent.Offset + "-" + (extent.Offset + extent.Size - 1) + ",";
                }
                headers.Add("Range", range.Substring(0, range.Length - 1));

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest(con, "GET", resource, headers);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }

                // parse the boundary from the content-type parameter. note that this will throw an exception if we don't get multipart
                // content in the response
                string boundary = new Regex(" boundary=\"?([^\\s]*)\"?;?").Match(resp.ContentType).Groups[1].Value;
                MultipartEntity entity = MultipartEntity.FromStream(resp.GetResponseStream(), boundary);

                resp.Close();
                return entity;
            }
            catch (KeyNotFoundException e)
            {
                throw new EsuException("Expected multipart response, but instead got " + resp.ContentType, e);
            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Deletes an object from the cloud.
        /// </summary>
        /// <param name="id">The identifier of the object to delete.</param>
        public void DeleteObject( Identifier id ) {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath(context, id);
                Uri u = buildUrl( resource );
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );
                if (id is ObjectKey) {
                    headers.Add("x-emc-pool", (id as ObjectKey).pool);
                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest( con, "DELETE", resource, headers );

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

            } catch( UriFormatException e ) {
                throw new EsuException( "Invalid URL", e );
            } catch( IOException e ) {
                throw new EsuException( "Error connecting to server", e );
            } catch( WebException e ) {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if( resp != null ) {
                    resp.Close();
                }
            }
        }

        /// <summary>
        /// Returns an object's ACL
        /// </summary>
        /// <param name="id">The identifier of the object whose ACL to read</param>
        /// <returns>The object's ACL</returns>
        public Acl GetAcl( Identifier id ) {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath(context, id) + "?acl";
                Uri u = buildUrl( resource );
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );
                if (id is ObjectKey) {
                    headers.Add("x-emc-pool", (id as ObjectKey).pool);
                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest( con, "GET", resource, headers );

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

                // Parse return headers.  User grants are in x-emc-useracl and
                // group grants are in x-emc-groupacl
                Acl acl = new Acl();
                readAcl( acl, resp.Headers["x-emc-useracl"], Grantee.GRANTEE_TYPE.USER );
                readAcl( acl, resp.Headers["x-emc-groupacl"], Grantee.GRANTEE_TYPE.GROUP );

                return acl;

            } catch( UriFormatException e ) {
                throw new EsuException( "Invalid URL", e );
            } catch( IOException e ) {
                throw new EsuException( "Error connecting to server", e );
            } catch( WebException e ) {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if( resp != null ) {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Sets the access control list on the object.
        /// </summary>
        /// <param name="id">The identifier of the object whose ACL to change</param>
        /// <param name="acl">The new ACL for the object.</param>
        public void SetAcl( Identifier id, Acl acl ) {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath(context, id) + "?acl";
                Uri u = buildUrl( resource );
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );
                if (id is ObjectKey) {
                    headers.Add("x-emc-pool", (id as ObjectKey).pool);
                }

                // Add acl
                if( acl != null ) {
                    processAcl( acl, headers );
                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest( con, "POST", resource, headers );

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

            } catch( UriFormatException e ) {
                throw new EsuException( "Invalid URL", e );
            } catch( IOException e ) {
                throw new EsuException( "Error connecting to server", e );
            } catch( WebException e ) {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if( resp != null ) {
                    resp.Close();
                }
            }
        }

        /// <summary>
        /// Writes the metadata into the object. If the tag does not exist, it is created and set to the corresponding value. If the tag exists, the existing value is replaced. 
        /// </summary>
        /// <param name="id">The identifier of the object to update</param>
        /// <param name="metadata">Metadata to write to the object.</param>
        public void SetUserMetadata( Identifier id, MetadataList metadata ) {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath(context, id) + "?metadata/user";
                Uri u = buildUrl( resource );
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );
                if (id is ObjectKey) {
                    headers.Add("x-emc-pool", (id as ObjectKey).pool);
                }

                // Process metadata
                if( metadata != null ) {
                    processMetadata( metadata, headers );
                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest( con, "POST", resource, headers );

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

            } catch( UriFormatException e ) {
                throw new EsuException( "Invalid URL", e );
            } catch( IOException e ) {
                throw new EsuException( "Error connecting to server", e );
            } catch( WebException e ) {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if( resp != null ) {
                    resp.Close();
                }
            }
        }

        /// <summary>
        /// Deletes metadata items from an object.
        /// </summary>
        /// <param name="id">The identifier of the object whose metadata to delete.</param>
        /// <param name="tags">The list of metadata tags to delete.</param>
        public void DeleteUserMetadata( Identifier id, MetadataTags tags ) {
            if( tags == null ) {
                throw new EsuException( "Must specify tags to delete" );
            }
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath(context, id) + "?metadata/user";
                Uri u = buildUrl( resource );
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );
                if (id is ObjectKey) {
                    headers.Add("x-emc-pool", (id as ObjectKey).pool);
                }

                // Add tags if needed
                processTags( tags, headers );


                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest( con, "DELETE", resource, headers );

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

            } catch( UriFormatException e ) {
                throw new EsuException( "Invalid URL", e );
            } catch( IOException e ) {
                throw new EsuException( "Error connecting to server", e );
            } catch( WebException e ) {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if( resp != null ) {
                    resp.Close();
                }
            }
        }

        /// <summary>
        /// Lists the versions of an object.
        /// </summary>
        /// <param name="id">The object whose versions to list.</param>
        /// <returns>The list of versions of the object.  If the object does not have any versions, the array will be empty.</returns>
        public List<ObjectId> ListVersions( Identifier id ) {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath(context, id) + "?versions";
                Uri u = buildUrl( resource );
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );
                if (id is ObjectKey) {
                    headers.Add("x-emc-pool", (id as ObjectKey).pool);
                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest( con, "GET", resource, headers );

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

                // Get object id list from response
                byte[] response = readResponse( resp, null, null );

                string responseStr = Encoding.UTF8.GetString( response );
                log.TraceEvent(TraceEventType.Verbose, 0,  "Response: " + responseStr );

                return parseVersionList( responseStr );

            } catch( UriFormatException e ) {
                throw new EsuException( "Invalid URL", e );
            } catch( IOException e ) {
                throw new EsuException( "Error connecting to server", e );
            } catch( WebException e ) {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if( resp != null ) {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a new immutable version of an object.
        /// </summary>
        /// <param name="id">The object to version</param>
        /// <returns>The id of the newly created version</returns>
        public ObjectId VersionObject( Identifier id ) {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath(context, id) + "?versions";
                Uri u = buildUrl( resource );
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );
                if (id is ObjectKey) {
                    headers.Add("x-emc-pool", (id as ObjectKey).pool);
                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest( con, "POST", resource, headers );

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

                // The new object ID is returned in the location response header
                string location = resp.Headers["location"];

                // Parse the value out of the URL

                MatchCollection m = OBJECTID_EXTRACTOR.Matches( location );
                if( m.Count > 0 ) {
                    string vid = m[0].Groups[1].Value;
                    log.TraceEvent(TraceEventType.Verbose, 0,  "Id: " + vid );
                    return new ObjectId( vid );
                } else {
                    throw new EsuException( "Could not find ObjectId in " + location );
                }
            } catch( UriFormatException e ) {
                throw new EsuException( "Invalid URL", e );
            } catch( IOException e ) {
                throw new EsuException( "Error connecting to server", e );
            } catch( WebException e ) {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if( resp != null ) {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Deletes a version from an object.  You cannot specify the base version
        /// of an object.
        /// </summary>
        /// <param name="vId">The ObjectID of the version to delete.</param>
        public void DeleteVersion(ObjectId vId) {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath(context, vId) + "?versions";
                Uri u = buildUrl( resource );
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest( con, "DELETE", resource, headers );

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }
            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }
        }

        /// <summary>
        /// Restores a version of an object to the base version (i.e. "promote" an 
        /// old version to the current version).
        /// </summary>
        /// <param name="id">Base object ID (target of the restore)</param>
        /// <param name="vId">Version object ID to restore</param>
        public void RestoreVersion(ObjectId id, ObjectId vId) {
            HttpWebResponse resp = null;
            try
            {
                string resource = getResourcePath(context, id) + "?versions";
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add("x-emc-uid", uid);

                // Set the version to promote
                headers.Add("x-emc-version-oid", vId.ToString());

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest(con, "PUT", resource, headers);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }
            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }
        }


        /// <summary>
        /// Lists all objects with the given tag.
        /// </summary>
        /// <param name="tag">Tag the tag to search for</param>
        /// <returns>The list of objects with the given tag.  If no objects are found the array will be empty.</returns>
        /// <exception cref="T:EsuApiLib.EsuException">if no objects are found (code 1003)</exception>
        public List<ObjectId> ListObjects( MetadataTag tag ) {
            return ListObjects(tag.Name);
        }

        /// <summary>
        /// Lists all objects with the given tag.
        /// </summary>
        /// <param name="tag">Tag the tag to search for</param>
        /// <param name="options">Options for listing the objects. After calling ListObjects, be sure to check the value of the token property to see if there are additional results.</param>
        /// <returns>The list of objects with the given tag.  If no objects are found the array will be empty.</returns>
        /// <exception cref="T:EsuApiLib.EsuException">if no objects are found (code 1003)</exception>
        public List<ObjectResult> ListObjects(MetadataTag tag, ListOptions options)
        {
            return ListObjects( tag.Name, options );
        }

        /// <summary>
        /// Lists all objects with the given tag.
        /// </summary>
        /// <param name="tag">Tag the tag to search for</param>
        /// <param name="options">Options for listing the objects. After calling ListObjects, be sure to check the value of the token property to see if there are additional results.</param>
        /// <returns>The list of objects with the given tag.  If no objects are found the array will be empty.</returns>
        /// <exception cref="T:EsuApiLib.EsuException">if no objects are found (code 1003)</exception>
        public List<ObjectResult> ListObjects(string tag, ListOptions options)
        {
            HttpWebResponse resp = null;
            try
            {
                string resource = context + "/objects";
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add("x-emc-uid", uid);
                if (utf8Enabled) headers.Add("x-emc-utf8", "true");

                // Add tag
                if (tag != null)
                {
                    headers.Add("x-emc-tags", utf8Enabled ? utf8Encode(tag) : tag);
                }
                else
                {
                    throw new EsuException("tag may not be null");
                }

                if (options != null)
                {
                    if (options.IncludeMetadata)
                    {
                        headers.Add("x-emc-include-meta", "1");
                        if (options.SystemMetadata != null)
                        {
                            headers.Add("x-emc-system-tags",
                                    join(options.SystemMetadata, ","));
                        }
                        if (options.UserMetadata != null)
                        {
                            headers.Add("x-emc-user-tags",
                                    join(options.UserMetadata, ","));
                        }
                    }
                    if (options.Limit > 0)
                    {
                        headers.Add("x-emc-limit", "" + options.Limit);
                    }
                    if (options.Token != null)
                    {
                        headers.Add("x-emc-token", options.Token);
                    }

                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest(con, "GET", resource, headers);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }

                // Check for token
                if (options != null)
                {
                    if (resp.Headers["x-emc-token"] != null)
                    {
                        options.Token = resp.Headers["x-emc-token"];
                    }
                    else
                    {
                        // No more results
                        options.Token = null;
                    }
                }
                else
                {
                    if (resp.Headers["x-emc-token"] != null)
                    {
                        // There are more results available, but no ListOptions
                        // object to receive the token. Issue a warning.
                        log.TraceEvent(TraceEventType.Warning, 1, "Results truncated.  Use ListOptions paramter to retrieve the token value for more results");
                    }
                }


                // Get object id list from response
                byte[] response = readResponse(resp, null, null);

                string responseStr = Encoding.UTF8.GetString(response);
                log.TraceEvent(TraceEventType.Verbose, 0, "Response: " + responseStr);

                return parseObjectListWithMetadata(responseStr);

            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }
            return null;
        }



        /// <summary>
        /// Lists all objects with the given tag.
        /// </summary>
        /// <param name="tag">Tag the tag to search for</param>
        /// <returns>The list of objects with the given tag.  If no objects are found the array will be empty.</returns>
        /// <exception cref="T:EsuApiLib.EsuException">if no objects are found (code 1003)</exception>
        public List<ObjectId> ListObjects( string tag ) {
            return getIds(ListObjects(tag, null));
        }


        /// <summary>
        /// Lists all objects with the given tag.  This method returns both the objects' IDs as well
        /// as their metadata.
        /// </summary>
        /// <param name="tag">Tag the tag to search for</param>
        /// <returns>The list of objects with the given tag.  If no objects are found the array will be empty.</returns>
        /// <exception cref="T:EsuApiLib.EsuException">if no objects are found (code 1003)</exception>
        public List<ObjectResult> ListObjectsWithMetadata(MetadataTag tag)
        {
            return ListObjectsWithMetadata(tag.Name);
        }

        /// <summary>
        /// Lists all objects with the given tag.  This method returns both the objects' IDs as well
        /// as their metadata.
        /// </summary>
        /// <param name="tag">Tag the tag to search for</param>
        /// <returns>The list of objects with the given tag.  If no objects are found the array will be empty.</returns>
        /// <exception cref="T:EsuApiLib.EsuException">if no objects are found (code 1003)</exception>
        public List<ObjectResult> ListObjectsWithMetadata(string tag)
        {
            ListOptions options = new ListOptions();
            options.IncludeMetadata = true;
            return ListObjects(tag, options);
        }

        /// <summary>
        /// Returns the set of listable tags for the current Tennant.
        /// </summary>
        /// <param name="tag">The tag whose children to list.  If null, only toplevel tags will be returned.</param>
        /// <returns>The list of listable tags.</returns>
        public MetadataTags GetListableTags( MetadataTag tag ) {
            return GetListableTags( tag.Name );
        }

        /// <summary>
        /// Returns the set of listable tags for the current Tennant.
        /// </summary>
        /// <param name="tag">The tag whose children to list.  If null, only toplevel tags will be returned.</param>
        /// <returns>The list of listable tags.</returns>
        public MetadataTags GetListableTags( string tag ) {
            HttpWebResponse resp = null;
            try {
                string resource = context + "/objects?listabletags";
                Uri u = buildUrl( resource );
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );
                if ( utf8Enabled ) headers.Add( "x-emc-utf8", "true" );

                // Add tag
                if( tag != null ) {
                    headers.Add( "x-emc-tags", utf8Enabled ? utf8Encode(tag) : tag );
                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest( con, "GET", resource, headers );

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

                // Get the user metadata tags out of x-emc-listable-tags
                MetadataTags tags = new MetadataTags();

                readTags( tags, resp.Headers["x-emc-listable-tags"], true );

                return tags;

            } catch( UriFormatException e ) {
                throw new EsuException( "Invalid URL", e );
            } catch( IOException e ) {
                throw new EsuException( "Error connecting to server", e );
            } catch( WebException e ) {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if( resp != null ) {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the list of user metadata tags assigned to the object.
        /// </summary>
        /// <param name="id">The object whose metadata tags to list</param>
        /// <returns>The list of user metadata tags assigned to the object</returns>
        public MetadataTags ListUserMetadataTags( Identifier id ) {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath(context, id) + "?metadata/tags";
                Uri u = buildUrl( resource );
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );
                if (utf8Enabled) headers.Add("x-emc-utf8", "true");
                if (id is ObjectKey) {
                    headers.Add("x-emc-pool", (id as ObjectKey).pool);
                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest( con, "GET", resource, headers );

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

                // Get the user metadata tags out of x-emc-listable-tags and
                // x-emc-tags
                MetadataTags tags = new MetadataTags();

                readTags( tags, resp.Headers["x-emc-listable-tags"], true );
                readTags( tags, resp.Headers["x-emc-tags"], false );

                return tags;

            } catch( UriFormatException e ) {
                throw new EsuException( "Invalid URL", e );
            } catch( IOException e ) {
                throw new EsuException( "Error connecting to server", e );
            } catch( WebException e ) {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if( resp != null ) {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Executes a query for objects matching the specified XQuery string.
        /// </summary>
        /// <param name="xquery">The XQuery string to execute against the cloud.</param>
        /// <returns>The list of objects matching the query.  If no objects are found, the array will be empty.</returns>
        public List<ObjectId> QueryObjects( string xquery ) {
            HttpWebResponse resp = null;
            try {
                string resource = context + "/objects";
                Uri u = buildUrl( resource );
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );

                // Add query
                if( xquery != null ) {
                    headers.Add( "x-emc-xquery", xquery );
                } else {
                    throw new EsuException( "Query cannot be null" );
                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest( con, "GET", resource, headers );

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

                // Get object id list from response
                byte[] response = readResponse( resp, null, null );

                string responseStr = Encoding.UTF8.GetString( response );
                log.TraceEvent(TraceEventType.Verbose, 0,  "Response: " + responseStr );

                return parseObjectList( responseStr );

            } catch( UriFormatException e ) {
                throw new EsuException( "Invalid URL", e );
            } catch( IOException e ) {
                throw new EsuException( "Error connecting to server", e );
            } catch( WebException e ) {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if( resp != null ) {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Lists the contents of a directory.
        /// </summary>
        /// <param name="path">the path to list.  Must be a directory.</param>
        /// <returns>the directory entries in the directory.</returns>
        public List<DirectoryEntry> ListDirectory(ObjectPath path)
        {
            return ListDirectory(path, null);
        }

        /// <summary>
        /// Lists the contents of a directory.
        /// </summary>
        /// <param name="path">the path to list.  Must be a directory.</param>
        /// <param name="options">Options for listing the objects. After calling 
        /// ListObjects, be sure to check the value of the token property to see 
        /// if there are additional results.</param>
        /// <returns>the directory entries in the directory.</returns>
        public List<DirectoryEntry> ListDirectory( ObjectPath path, ListOptions options ) {
            HttpWebResponse resp = null;
            if (!path.IsDirectory())
            {
                throw new EsuException("listDirectory must be called with a directory path");
            }

            try {

                string resource = getResourcePath(context, path);
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add("x-emc-uid", uid);
                if (utf8Enabled) headers.Add("x-emc-utf8", "true");

                if (options != null)
                {
                    if (options.IncludeMetadata)
                    {
                        headers.Add("x-emc-include-meta", "true");
                        if (options.SystemMetadata != null)
                        {
                            headers.Add("x-emc-system-tags",
                                    join(options.SystemMetadata, ","));
                        }
                        if (options.UserMetadata != null)
                        {
                            headers.Add("x-emc-user-tags",
                                    join(options.UserMetadata, ","));
                        }
                    }
                    if (options.Limit > 0)
                    {
                        headers.Add("x-emc-limit", "" + options.Limit);
                    }
                    if (options.Token != null)
                    {
                        headers.Add("x-emc-token", options.Token);
                    }
                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest(con, "GET", resource, headers);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }

                byte[] responseBuffer = readResponse(resp, null, null);

                // Check for token
                if (options != null)
                {
                    if (resp.Headers["x-emc-token"] != null)
                    {
                        options.Token = resp.Headers["x-emc-token"];
                    }
                    else
                    {
                        // No more results
                        options.Token = null;
                    }
                }
                else
                {
                    if (resp.Headers["x-emc-token"] != null)
                    {
                        // There are more results available, but no ListOptions
                        // object to receive the token. Issue a warning.
                        log.TraceEvent(TraceEventType.Warning, 1, "Results truncated.  Use ListOptions paramter to retrieve the token value for more results");
                    }
                }

                return parseDirectoryList(responseBuffer, path);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }
            return null;

        }


        /// <summary>
        /// Returns all of an object's metadata and its ACL in
        /// one call.
        /// </summary>
        /// <param name="id">the object's identifier.</param>
        /// <returns>the object's metadata</returns>
        public ObjectMetadata GetAllMetadata( Identifier id ) {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath(context, id);
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add("x-emc-uid", uid);
                if (utf8Enabled) headers.Add("x-emc-utf8", "true");
                if (id is ObjectKey) {
                    headers.Add("x-emc-pool", (id as ObjectKey).pool);
                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest(con, "HEAD", resource, headers);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299) {
                    handleError(resp);
                }

                // Parse return headers.  Regular metadata is in x-emc-meta and
                // listable metadata is in x-emc-listable-meta
                MetadataList meta = new MetadataList();
                readMetadata(meta, resp.Headers["x-emc-meta"], false);
                readMetadata(meta, resp.Headers["x-emc-listable-meta"], true);

                // Parse return headers.  User grants are in x-emc-useracl and
                // group grants are in x-emc-groupacl
                Acl acl = new Acl();
                readAcl(acl, resp.Headers["x-emc-useracl"], Grantee.GRANTEE_TYPE.USER);
                readAcl(acl, resp.Headers["x-emc-groupacl"], Grantee.GRANTEE_TYPE.GROUP);

                ObjectMetadata om = new ObjectMetadata();
                om.ACL = acl;
                om.Metadata = meta;

                return om;

            } catch (UriFormatException e) {
                throw new EsuException("Invalid URL", e);
            } catch (IOException e) {
                throw new EsuException("Error connecting to server", e);
            } catch (WebException e) {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null) {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// An Atmos user (UID) can construct a pre-authenticated URL to an 
        /// object, which may then be used by anyone to retrieve the 
        /// object (e.g., through a browser). This allows an Atmos user 
        /// to let a non-Atmos user download a specific object. The 
        /// entire object/file is read.
        /// </summary>
        /// <param name="id">the object to generate the URL for</param>
        /// <param name="expiration">expiration the expiration date of the URL.  Note, be sure to ensure your expiration is in UTC (DateTimeKind.Utc)</param>
        /// <returns>a URL that can be used to share the object's content</returns>
        public Uri getShareableUrl(Identifier id, DateTime expiration)
        {
            return GetShareableUrl(id, expiration);
        }

        /// <summary>
        /// An Atmos user (UID) can construct a pre-authenticated URL to an 
        /// object, which may then be used by anyone to retrieve the 
        /// object (e.g., through a browser). This allows an Atmos user 
        /// to let a non-Atmos user download a specific object. The 
        /// entire object/file is read.
        /// </summary>
        /// <param name="id">the object to generate the URL for</param>
        /// <param name="expiration">expiration the expiration date of the URL.  Note, be sure to ensure your expiration is in UTC (DateTimeKind.Utc)</param>
        /// <returns>a URL that can be used to share the object's content</returns>
        public Uri GetShareableUrl(Identifier id, DateTime expiration)
        {
            return GetShareableUrl(id, expiration, null);
        }

        /// <summary>
        /// Creates a shareable URL with the specified content-disposition.  This disposition value will be returned in the Content-Disposition response header.
        /// </summary>
        /// <param name="id">the object to generate the URL for</param>
        /// <param name="expiration">expiration the expiration date of the URL.  Note, be sure to ensure your expiration is in UTC (DateTimeKind.Utc)</param>
        /// <param name="disposition">the value that will be sent by the server in the Content-Disposition response header</param>
        /// <returns>a URL that can be used to share the object's content</returns>
        public Uri GetShareableUrl(Identifier id, DateTime expiration, string disposition) {
            if (id is ObjectKey) throw new Exception("You cannot create a shareable URL with an object key");

            string resource = getResourcePath(context, id);
            string uidEnc = Uri.EscapeDataString(uid);
            string unixTime = (expiration - new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)).TotalSeconds.ToString( "F0" );

            StringBuilder sb = new StringBuilder();
            sb.Append("GET\n");
            sb.Append(resource.ToLower() + "\n");
            sb.Append(uid + "\n");

            sb.Append("" + unixTime);

            if (disposition != null)
            {
                sb.Append("\n" + disposition);
            }

            string signature = Uri.EscapeDataString(sign(Encoding.UTF8.GetBytes(sb.ToString())));
            resource += "?uid=" + uidEnc + "&expires=" + unixTime +
                "&signature=" + signature;
            if (disposition != null)
            {
                resource += "&disposition=" + Uri.EscapeDataString(disposition);
            }

            Uri u = buildUrl(resource);

            return u;

        }

        /// <summary>
        /// Renames a file or directory within the namespace.
        /// </summary>
        /// <param name="source">The file or directory to rename</param>
        /// <param name="destination">The new path for the file or directory</param>
        /// <param name="force">If true, the desination file or 
        /// directory will be overwritten.  Directories must be empty to be 
        /// overwritten.</param>
        public void Rename(ObjectPath source, ObjectPath destination, bool force) {
            HttpWebResponse resp = null;
            try
            {
                string resource = getResourcePath(context, source) + "?rename";
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add("x-emc-uid", uid);
                if (utf8Enabled) headers.Add("x-emc-utf8", "true");
            
                string destPath = destination.ToString();
                if (destPath.StartsWith("/"))
                {
                    destPath = destPath.Substring(1);
                }
                headers.Add("x-emc-path", utf8Enabled ? utf8Encode(destPath) : destPath);

                if (force)
                {
                    headers.Add("x-emc-force", "true");
                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest(con, "POST", resource, headers);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }

            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }
        }

        /// <summary>
        /// Gets information about the connected service.  Currently, this is
        /// only the version of Atmos.
        /// </summary>
        /// <returns></returns>
        public ServiceInformation GetServiceInformation()
        {
            HttpWebResponse resp = null;
            try
            {
                string resource = context + "/service";
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add("x-emc-uid", uid);

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest(con, "GET", resource, headers);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }

                byte[] response = readResponse(resp, null, null);

                string responseStr = Encoding.UTF8.GetString(response);
                log.TraceEvent(TraceEventType.Verbose, 0, "Response: " + responseStr);
                return parseServiceInformation(responseStr, resp.Headers);

            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }

            return null;
        }

        /// <summary>
        /// Gets Replica, Expiration, and Retention information for an object
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ObjectInfo GetObjectInfo(Identifier id)
        {
            HttpWebResponse resp = null;
            try
            {
                string resource = getResourcePath(context, id) + "?info";
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add("x-emc-uid", uid);
                if (id is ObjectKey) {
                    headers.Add("x-emc-pool", (id as ObjectKey).pool);
                }

                 // Add date
                addDateHeader(headers);

                // Sign request
                signRequest(con, "GET", resource, headers);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }

                byte[] response = readResponse(resp, null, null);

                ObjectInfo info = new ObjectInfo(Encoding.UTF8.GetString(response));
                return info;

            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }
            return null;
            
        }




        /// <summary>
        /// Gets the UID used for this object's connections.
        /// </summary>
        /// <returns>The connection's UID</returns>
        public string GetUid() {
            return uid;
        }

        /// <summary>
        /// Gets the name or IP of the host this object connects to.
        /// </summary>
        /// <returns>The hostname or IP as a string</returns>
        public string GetHost() {
            return host;
        }

        /// <summary>
        /// Gets the port number this object connects to.
        /// </summary>
        /// <returns>The port number</returns>
        public int GetPort() {
            return port;
        }

        /// <summary>
        /// Creates an anonymous access token using the specified policy and ACL
        /// </summary>
        /// <param name="id">identifier of the target object for the access token.</param>
        /// <param name="policy">the token policy for the new access token.</param>
        /// <param name="acl">the ACL that will be assigned to objects created using this access token.</param>
        /// <returns>The URL of the access token.</returns>
        public Uri CreateAccessToken(Identifier id, PolicyType policy, Acl acl)
        {
            HttpWebResponse resp = null;
            try
            {
                string resource = context + "/accesstokens";
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add("Content-Type", "application/xml");
                headers.Add("x-emc-uid", uid);

                // Add id
                if (id != null)
                {
                    if (id is ObjectId) headers.Add("x-emc-objectid", id.ToString());
                    else if (id is ObjectPath) headers.Add("x-emc-path", id.ToString());
                    else throw new EsuException("Only object ID and path are supported with access tokens");
                }

                // Add acl
                if (acl != null)
                {
                    processAcl(acl, headers);
                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest(con, "POST", resource, headers);

                // serialize XML
                Stream memStream = new MemoryStream();
                XmlSerializer serializer = new XmlSerializer(typeof(PolicyType));
                serializer.Serialize(memStream, policy);
                memStream.Position = 0;

                // post data
                writeRequestBodyFromStream(con, memStream, memStream.Length);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }

                // The token URL is returned in the location response header
                string location = resp.Headers["location"];

                return buildUrl(location);
            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves details about the specified access token. Implementation simply extracts the token ID from the URL and calls GetAccessToken(String).
        /// </summary>
        /// <param name="tokenUri">The URL of the access token.</param>
        /// <returns></returns>
        public AccessTokenType GetAccessToken(Uri tokenUri)
        {
            string path = tokenUri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            return GetAccessToken(path.Split('/').Last());
        }

        /// <summary>
        /// Retrieves details about the specified access token.
        /// </summary>
        /// <param name="tokenId">The ID of the access token.</param>
        /// <returns></returns>
        public AccessTokenType GetAccessToken(string tokenId)
        {
            HttpWebResponse resp = null;
            try
            {
                string resource = context + "/accesstokens/" + tokenId + "?info";
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add("x-emc-uid", uid);

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest(con, "GET", resource, headers);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }

                XmlSerializer serializer = new XmlSerializer(typeof(AccessTokenType));
                AccessTokenType accessToken = (AccessTokenType) serializer.Deserialize(resp.GetResponseStream());
                resp.Close();

                return accessToken;
            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Deletes the specified access token. Implementation simply extracts the token ID from the URL and calls deleteAccessToken(String).
        /// </summary>
        /// <param name="tokenUri">The URL of the access token.</param>
        public void DeleteAccessToken(Uri tokenUri)
        {
            string path = tokenUri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            DeleteAccessToken(path.Split('/').Last());
        }

        /// <summary>
        /// Deletes the specified access token.
        /// </summary>
        /// <param name="tokenId">The ID of the access token.</param>
        public void DeleteAccessToken(string tokenId)
        {
            HttpWebResponse resp = null;
            try
            {
                string resource = context + "/accesstokens/" + tokenId;
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add("x-emc-uid", uid);

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest(con, "DELETE", resource, headers);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }

            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }
        }

        /// <summary>
        /// Lists all access tokens owned by the user using the options provided.
        /// </summary>
        /// <param name="options">Options for listing the objects. After calling 
        /// ListAccessTokens, be sure to check the value of the token property to see 
        /// if there are additional results.</param>
        /// <returns>The list of access tokens that the user has created</returns>
        public List<AccessTokenType> ListAccessTokens(ListOptions options)
        {
            HttpWebResponse resp = null;
            try
            {
                string resource = context + "/accesstokens";
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add("x-emc-uid", uid);

                if (options != null)
                {
                    if (options.Limit > 0)
                    {
                        headers.Add("x-emc-limit", "" + options.Limit);
                    }
                    if (options.Token != null)
                    {
                        headers.Add("x-emc-token", options.Token);
                    }

                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest(con, "GET", resource, headers);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }

                // Check for token
                if (options != null)
                {
                    if (resp.Headers["x-emc-token"] != null)
                    {
                        options.Token = resp.Headers["x-emc-token"];
                    }
                    else
                    {
                        // No more results
                        options.Token = null;
                    }
                }
                else
                {
                    if (resp.Headers["x-emc-token"] != null)
                    {
                        // There are more results available, but no ListOptions
                        // object to receive the token. Issue a warning.
                        log.TraceEvent(TraceEventType.Warning, 1, "Results truncated.  Use ListOptions paramter to retrieve the token value for more results");
                    }
                }

                XmlSerializer serializer = new XmlSerializer(typeof(ListAccessTokenResultType));
                ListAccessTokenResultType result = (ListAccessTokenResultType)serializer.Deserialize(resp.GetResponseStream());
                resp.Close();

                return new List<AccessTokenType>(result.AccessTokensList);
            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a new Atmos subtenant in ECS.
        /// <b>Note:</b> this API is only applicable to ECS and not to native Atmos.
        /// Also, note that when calling this API, the UID set on the EsuRestApi object should be a "bare" UID without a prefixing subtenant ID.
        /// </summary>
        /// <param name="replicationGroupId">Optional.  The ECS Replication Group ID to use for the subtenant.  If null, the default replication group from the Namespace will be used.</param>
        /// <returns>The new subtenant ID.</returns>
        public string CreateSubtenant(string replicationGroupId)
        {
            HttpWebResponse resp = null;
            try
            {
                string resource = context + "/subtenant";
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add("x-emc-uid", uid);

                if (replicationGroupId != null)
                {
                    headers.Add("x-emc-vpool", replicationGroupId);
                }

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest(con, "PUT", resource, headers);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }
                string subId = resp.Headers["subtenantID"];

                resp.Close();
                resp = null;


                return subId;
            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }
            return null;

        }

        /// <summary>
        /// Creates a new Atmos subtenant in ECS using the default Replication Group from the Namespace.
        /// <b>Note:</b> this API is only applicable to ECS and not to native Atmos.
        /// Also, note that when calling this API, the UID set on the EsuRestApi object should be a "bare" UID without a prefixing subtenant ID.
        /// </summary>
        /// <returns>The new subtenant ID.</returns>
        public string CreateSubtenant()
        {
            return CreateSubtenant(null);
        }

        /// <summary>
        /// Deletes an Atmos subtenant from ECS.
        /// <b>Note:</b> this API is only applicable to ECS and not to native Atmos.
        /// Also, note that when calling this API, the UID set on the EsuRestApi object should be a "bare" UID without a prefixing subtenant ID.
        /// </summary>
        /// <param name="subtenantId">The ID of the subtenant to delete</param>
        public void DeleteSubtenant(string subtenantId)
        {
            HttpWebResponse resp = null;
            try
            {
                string resource = context + "/subtenant/" + subtenantId;
                Uri u = buildUrl(resource);
                HttpWebRequest con = createWebRequest(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add("x-emc-uid", uid);

                // Add date
                addDateHeader(headers);

                // Sign request
                signRequest(con, "DELETE", resource, headers);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }
                resp.Close();
                resp = null;
            }
            catch (UriFormatException e)
            {
                throw new EsuException("Invalid URL", e);
            }
            catch (IOException e)
            {
                throw new EsuException("Error connecting to server", e);
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    handleError((HttpWebResponse)e.Response);
                }
                else
                {
                    throw new EsuException("Error executing request: " + e.Message, e);
                }
            }
            finally
            {
                if (resp != null)
                {
                    resp.Close();
                }
            }

        }

        #endregion

        #region PrivateMethods
        /**
         * Builds a new URL to the given resource
         */
        protected virtual Uri buildUrl( string resource ) {
            return new Uri( protocol + "://" + host + ":" + port + resource );
        }

        private void processMetadata( MetadataList metadata, Dictionary<string, string> headers ) {
            StringBuilder listable = new StringBuilder();
            StringBuilder nonListable = new StringBuilder();

            log.TraceEvent(TraceEventType.Verbose, 0,  "Processing " + metadata.Count() + " metadata entries" );

            foreach( Metadata meta in metadata ) {
                if( meta.Listable ) {
                    if( listable.Length > 0 ) {
                        listable.Append( ", " );
                    }
                    listable.Append( formatTag( meta ) );
                } else {
                    if( nonListable.Length > 0 ) {
                        nonListable.Append( ", " );
                    }
                    nonListable.Append( formatTag( meta ) );
                }
            }
            
            if( metadata.getExpirationPeriod() != null )
                headers.Add( "x-emc-expiration-period", metadata.getExpirationPeriod().Value.ToString() );
            if (metadata.getRetentionPeriod() != null)
                headers.Add( "x-emc-retention-period", metadata.getRetentionPeriod().Value.ToString() );

            // Only set the headers if there's data
            if( listable.Length > 0 ) {
                headers.Add( "x-emc-listable-meta", listable.ToString() );
            }
            if( nonListable.Length > 0 ) {
                headers.Add( "x-emc-meta", nonListable.ToString() );
            }
            if (utf8Enabled && !headers.ContainsKey("x-emc-utf8")) {
                headers.Add("x-emc-utf8", "true");
            }

        }

        private string formatTag( Metadata meta ) {
            string name = utf8Enabled ? utf8Encode(meta.Name) : meta.Name;
            string value = meta.Value == null ? String.Empty : meta.Value;
            if (utf8Enabled) value = utf8Encode(value);
            else value = value.Replace(",", "").Replace("\n", ""); // strip commas and newlines for now.
            return name + "=" + value;
        }

        private void processAcl( Acl acl, Dictionary<string, string> headers ) {
            StringBuilder userGrants = new StringBuilder();
            StringBuilder groupGrants = new StringBuilder();

            foreach( Grant grant in acl ) {
                if( grant.Grantee.GranteeType == Grantee.GRANTEE_TYPE.USER ) {
                    if( userGrants.Length > 0 ) {
                        userGrants.Append( "," );
                    }
                    userGrants.Append( grant.ToString() );
                } else {
                    if( groupGrants.Length > 0 ) {
                        groupGrants.Append( "," );
                    }
                    groupGrants.Append( grant.ToString() );
                }
            }

            headers.Add( "x-emc-useracl", userGrants.ToString() );
            headers.Add( "x-emc-groupacl", groupGrants.ToString() );

        }

        private void signRequest( HttpWebRequest con, string method, string resource, Dictionary<string, string> headers ) {
            // Build the string to hash.
            StringBuilder hashStr = new StringBuilder();
            MemoryStream ms = new MemoryStream();

            hashStr.Append( method + "\n" );

            // If content type exists, add it.  Otherwise add a blank line.
            if( headers.ContainsKey( "Content-Type" ) ) {
                log.TraceEvent(TraceEventType.Verbose, 0,  "Content-Type: " + headers["Content-Type"] );
                hashStr.Append( headers["Content-Type"] + "\n" );
            } else {
                hashStr.Append( "\n" );
            }

            // If the range header exists, add it.  Otherwise add a blank line.
            if( headers.ContainsKey( "Range" ) ) {
                hashStr.Append( headers["Range"] + "\n" );
            } else {
                hashStr.Append( "\n" );
            }

            // Add the current date and the resource.
            hashStr.Append(headers["Date"] + "\n");
            hashStr.Append( resource.ToLower() + "\n" );

            // Up to here, we're using UTF-8 to encode the path.  Other headers will
            // Be ISO-8859-1, so write the bytes and start over.
            byte[] utfbytes = Encoding.UTF8.GetBytes(hashStr.ToString());
            ms.Write(utfbytes, 0, utfbytes.Length);
            hashStr = new StringBuilder();

            // Do the 'x-emc' headers.  The headers must be hashed in alphabetic
            // order and the values must be stripped of whitespace and newlines.
            List<string> keys = new List<string>();
            Dictionary<string, string> newheaders = new Dictionary<string, string>();

            // Extract the keys and values
            foreach( string key in headers.Keys ) {
                if( key.IndexOf( "x-emc" ) == 0 ) {
                    keys.Add( key.ToLower() );
                    newheaders.Add( key.ToLower(), normalizeHeaderValue(headers[key]) );
                }
            }

            // Sort the keys and add the headers to the hash string.
            keys.Sort();
            bool first = true;
            foreach( string key in keys ) {
                if( !first ) {
                    hashStr.Append( "\n" );
                } else {
                    first = false;
                }
                //this.trace( "xheader: " . k . "." . newheaders[k] );
                hashStr.Append( key + ':' + newheaders[key] );
            }
            byte[] latinbytes = headerEncoder.GetBytes(hashStr.ToString());
            ms.Write(latinbytes, 0, latinbytes.Length);

            string hashOut = sign(ms.ToArray());

            // add custom headers (i.e. for 3rd party authentication proxy)
            if (customHeaders != null) headers = headers.Concat(customHeaders).ToDictionary(e => e.Key, e => e.Value);

            // Can set all the headers, etc now.  Microsoft doesn't let you
            // set some of the headers directly.  Modify the headers through
            // reflection to get around this.
            MethodInfo m = con.Headers.GetType().GetMethod( "AddWithoutValidate", BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Instance, null,
                new Type[] { typeof( string ), typeof( string ) }, null );

            foreach( string name in headers.Keys ) {
                log.TraceEvent(TraceEventType.Verbose, 0,  "Setting " + name );
                m.Invoke( con.Headers, new object[] { name, headers[name] } );
            }

            // Set the signature header
            con.Headers["x-emc-signature"] = hashOut;

            // Set the method.
            con.Method = method;

        }

        private string normalizeHeaderValue(string p)
        {
            p = p.Replace( "\n", "" );

            int len = p.Length;
            
            // Normalize consecutive spaces to one space.
            while (true)
            {
                p = p.Replace("  ", " ");
                if (p.Length == len)
                {
                    return p;
                }
                len = p.Length;
            }

        }

        private string sign(byte[] hashBytes) {
            // Compute the signature hash
            HMACSHA1 mac = new HMACSHA1(secret);
            log.TraceEvent(TraceEventType.Verbose, 0, hashBytes.Length + " bytes to hash");
            mac.TransformFinalBlock(hashBytes, 0, hashBytes.Length);
            byte[] hashData = mac.Hash;
            log.TraceEvent(TraceEventType.Verbose, 0, hashData.Length + " bytes in signature");
            // Encode the hash in Base64.
            string hashOut = Convert.ToBase64String(hashData);

            log.TraceEvent(TraceEventType.Verbose, 0, "Hash: " + hashOut);
            return hashOut;
        }

        private void handleError( HttpWebResponse resp ) {
            // Try and read the response body.
            try {
                byte[] response = readResponse( resp, null, null );
                string responseText = Encoding.UTF8.GetString( response );
                log.TraceEvent(TraceEventType.Verbose, 0,  "Error response: " + responseText );
                XmlDocument d = new XmlDocument();
                d.LoadXml( responseText );

                if( d.GetElementsByTagName( "Code" ).Count == 0 ||
                    d.GetElementsByTagName( "Message" ).Count == 0 ) {
                    // not an error from ESU
                    throw new EsuException( resp.StatusDescription, (int)resp.StatusCode );
                }

                string code = d.GetElementsByTagName( "Code" ).Item( 0 ).InnerText;
                string message = d.GetElementsByTagName( "Message" ).Item( 0 ).InnerText;


                log.TraceEvent(TraceEventType.Verbose, 0,  "Error: " + code + " message: " + message );
                throw new EsuException( message, int.Parse( code ) );

            } catch( IOException e ) {
                log.TraceEvent(TraceEventType.Verbose, 0,  "Could not read error response body: " + e );
                // Just throw what we know from the response
                try {
                    throw new EsuException( resp.StatusDescription, (int)resp.StatusCode );
                } catch( IOException e1 ) {
                    log.TraceEvent(TraceEventType.Verbose, 0,  "Could not get response code/message: " + e1 );
                    throw new EsuException( "Could not get response code", e1 );
                }
            } catch( XmlException e ) {
                try {
                    log.TraceEvent(TraceEventType.Verbose, 0,  "Could not parse response body for " +
                            resp.StatusCode + ": " + resp.StatusDescription + ": " +
                            e );
                    throw new EsuException( "Could not parse response body for " +
                            resp.StatusCode + ": " + resp.StatusDescription,
                            e, (int)resp.StatusCode);
                } catch( IOException e1 ) {
                    throw new EsuException( "Could not parse response body", e1 );
                }

            }

        }

        private byte[] readResponse( HttpWebResponse resp, byte[] buffer, Extent extent ) {
            Stream rs = resp.GetResponseStream();
            try {
                byte[] output;
                int contentLength = (int)resp.ContentLength;
                // If we know the content length, read it directly into a buffer.
                if( contentLength != -1 || extent != null ) {
                    if( buffer != null && contentLength != -1 && buffer.Length < contentLength ) {
                        throw new EsuException( "The response buffer was not long enough to hold the response: " + buffer.Length + "<" + contentLength );
                    }
                    else if (extent != null && buffer != null && buffer.Length < extent.Size)
                    {
                        throw new EsuException("The response buffer was not long enough to hold the response: " + buffer.Length + "<" + extent.Size);
                    }
                    if (extent != null && extent.Size > int.MaxValue)
                    {
                        throw new EsuException("Cannot read more than " + int.MaxValue + " bytes into a buffer.  Use ReadObjectStream instead.");
                    }
                    if( buffer != null ) {
                        output = buffer;
                    } else {
                        if (contentLength != -1)
                        {
                            output = new byte[(int)resp.ContentLength];
                        }
                        else // (extent != null)
                        {
                            output = new byte[extent.Size];
                        }
                    }

                    int bytesToRead = 0;
                    if (contentLength != -1)
                    {
                        bytesToRead = contentLength;
                    }
                    else
                    {
                        bytesToRead = (int)extent.Size;
                    }
                    int c = 0;
                    while( c < bytesToRead ) {
                        int read = rs.Read(output, c, bytesToRead - c);
                        if( read == 0 ) {
                            // EOF!
                            throw new EsuException("EOF reading response at position " + c + " size " + (bytesToRead - c));
                        }
                        c += read;
                    }

                    return output;
                } else {
                    log.TraceEvent(TraceEventType.Verbose, 0,  "Content length is unknown.  Buffering output." );
                    // Else, use a MemoryStream to collect the response.
                    if( buffer == null ) {
                        buffer = new byte[4096];
                    }
                    MemoryStream ms = new MemoryStream();
                    int c = 0;
                    while( (c = rs.Read( buffer, 0, buffer.Length )) != 0 ) {
                        ms.Write( buffer, 0, c );
                    }
                    ms.Close();
                    return ms.ToArray();
                }
            } finally {
                rs.Close();
            }

        }

        private void processTags( MetadataTags tags, Dictionary<string, string> headers ) {
            StringBuilder taglist = new StringBuilder();

            log.TraceEvent(TraceEventType.Verbose, 0,  "Processing " + tags.Count() + " metadata tag entries" );

            foreach( MetadataTag tag in tags ) {
                if( taglist.Length > 0 ) {
                    taglist.Append( "," );
                }
                taglist.Append( utf8Enabled ? utf8Encode(tag.Name) : tag.Name );
            }

            if( taglist.Length > 0 ) {
                headers.Add( "x-emc-tags", taglist.ToString() );
            }

            if ( utf8Enabled && !headers.ContainsKey("x-emc-utf8") ) {
                headers.Add("x-emc-utf8", "true");
            }
        }

        private string utf8Encode( string rawValue ) {
            return Uri.EscapeDataString( rawValue ).Replace( "+", "%20" );
        }

        private string utf8Decode(string encodedValue) {
            return Uri.UnescapeDataString(encodedValue);
        }

        private void readMetadata( MetadataList meta, string header, bool listable ) {
            if( header == null ) {
                return;
            }

            string[] attrs = header.Split( new string[] { "," }, StringSplitOptions.RemoveEmptyEntries );
            for( int i = 0; i < attrs.Length; i++ ) {
                string[] nvpair = attrs[i].Split( new string[] { "=" }, 2, StringSplitOptions.RemoveEmptyEntries );
                string name = nvpair[0];
                string value = "";
                if (nvpair.Length > 1)
                {
                    value = nvpair[1];
                }

                name = name.Trim();

                if (utf8Enabled)
                {
                    name = utf8Decode(name);
                    value = utf8Decode(value);
                }

                Metadata m = new Metadata( name, value, listable );
                log.TraceEvent(TraceEventType.Verbose, 0,  "Meta: " + m );
                meta.AddMetadata( m );
            }

        }


        private void readAcl( Acl acl, string header, EsuApiLib.Grantee.GRANTEE_TYPE type ) {
            log.TraceEvent(TraceEventType.Verbose, 0,  "readAcl: " + header );
            string[] grants = header.Split( new string[] { "," }, StringSplitOptions.RemoveEmptyEntries );
            for( int i = 0; i < grants.Length; i++ ) {
                string[] nvpair = grants[i].Split( new string[] { "=" }, 2, StringSplitOptions.RemoveEmptyEntries );
                string grantee = nvpair[0];
                string permission = nvpair[1];

                grantee = grantee.Trim();

                // Currently, the server returns "FULL" instead of "FULL_CONTROL".
                // For consistency, change this to value use in the request
                if( "FULL".Equals( permission ) ) {
                    permission = Permission.FULL_CONTROL;
                }

                log.TraceEvent(TraceEventType.Verbose, 0,  "grant: " + grantee + "." + permission + " (" + type
                        + ")" );

                Grantee ge = new Grantee( grantee, type );
                Grant gr = new Grant( ge, permission );
                log.TraceEvent(TraceEventType.Verbose, 0,  "Grant: " + gr );
                acl.AddGrant( gr );
            }
        }

        private void readTags( MetadataTags tags, string header, bool listable ) {
            if( header == null ) {
                return;
            }

            string[] attrs = header.Split( new string[] { "," }, StringSplitOptions.RemoveEmptyEntries );
            for( int i = 0; i < attrs.Length; i++ ) {
                string attr = attrs[i].Trim();
                tags.AddTag( new MetadataTag( utf8Enabled ? utf8Decode(attr) : attr, listable ) );
            }

        }

        private List<ObjectId> parseObjectList(string responseStr)
        {
            List<ObjectId> objs = new List<ObjectId>();
            try
            {
                XmlDocument d = new XmlDocument();
                d.LoadXml(responseStr);

                XmlNodeList olist = d.GetElementsByTagName("ObjectID");
                log.TraceEvent(TraceEventType.Verbose, 0, "Found " + olist.Count + " results");
                foreach (XmlNode xn in olist)
                {
                    objs.Add(new ObjectId(xn.InnerText));
                }

            }
            catch (XmlException e)
            {
                throw new EsuException("Error parsing xml object list", e);
            }

            return objs;

        }

        private List<ObjectId> parseVersionList(string responseStr)
        {
            List<ObjectId> objs = new List<ObjectId>();
            try
            {
                XmlDocument d = new XmlDocument();
                d.LoadXml(responseStr);

                XmlNodeList olist = d.GetElementsByTagName("OID");
                log.TraceEvent(TraceEventType.Verbose, 0, "Found " + olist.Count + " results");
                foreach (XmlNode xn in olist)
                {
                    objs.Add(new ObjectId(xn.InnerText));
                }

            }
            catch (XmlException e)
            {
                throw new EsuException("Error parsing xml object list", e);
            }

            return objs;

        }

        private XmlNode getChildByName(string name, XmlNodeList children)
        {
            foreach (XmlNode node in children)
            {
                if (node.LocalName.Equals(name))
                {
                    return node;
                }
            }
            return null;
        }

        private List<DirectoryEntry> parseDirectoryList(byte[] dir, ObjectPath path)
        {
            XmlDocument d = new XmlDocument();
            String s = Encoding.UTF8.GetString(dir);
            d.LoadXml(s);

            List<DirectoryEntry> entries = new List<DirectoryEntry>();
            log.TraceEvent(TraceEventType.Verbose, 0, Encoding.UTF8.GetString(dir));
            XmlNodeList olist = d.GetElementsByTagName("DirectoryEntry");
            log.TraceEvent(TraceEventType.Verbose, 0, "Found " + olist.Count + " objects in directory");
            foreach (XmlNode xn in olist)
            {
                DirectoryEntry de = new DirectoryEntry();
                string name = null;
                foreach (XmlNode child in xn.ChildNodes)
                {
                    if (child.LocalName.Equals("ObjectID"))
                    {
                        de.Id = new ObjectId(child.InnerText);
                    }
                    else if (child.LocalName.Equals("Filename"))
                    {
                        name = child.InnerText;
                    }
                    else if (child.LocalName.Equals("FileType"))
                    {
                        de.Type = child.InnerText;
                    }
                }

                if (name == null)
                {
                    throw new EsuException("Could not find object name in directory!");
                }
                if ("directory".Equals(de.Type))
                {
                    name += "/";
                }

                de.Path = new ObjectPath(path.ToString() + name);

                // Next, get the metadata
                de.SystemMetadata = new MetadataList();
                de.UserMetadata = new MetadataList();
                XmlNode sMetaNode = getChildByName("SystemMetadataList", xn.ChildNodes);
                XmlNode uMetaNode = getChildByName("UserMetadataList", xn.ChildNodes);

                if (sMetaNode != null)
                {
                    foreach (XmlNode metaNode in sMetaNode.ChildNodes)
                    {
                        if (!metaNode.LocalName.Equals("Metadata"))
                        {
                            continue;
                        }

                        string mName = getChildByName("Name", metaNode.ChildNodes).InnerText;
                        string mValue = getChildByName("Value", metaNode.ChildNodes).InnerText;

                        de.SystemMetadata.AddMetadata(new Metadata(mName, mValue, false));
                    }
                }

                if (uMetaNode != null)
                {
                    foreach (XmlNode metaNode in uMetaNode.ChildNodes)
                    {
                        if (!metaNode.LocalName.Equals("Metadata"))
                        {
                            continue;
                        }

                        string mName = getChildByName("Name", metaNode.ChildNodes).InnerText;
                        string mValue = getChildByName("Value", metaNode.ChildNodes).InnerText;
                        string mListable = getChildByName("Listable", metaNode.ChildNodes).InnerText;

                        de.UserMetadata.AddMetadata(new Metadata(mName, mValue, "true".Equals(mListable)));
                    }
                }



                entries.Add(de);
            }

            return entries;
        }


        private List<ObjectResult> parseObjectListWithMetadata(string responseStr)
        {
            List<ObjectResult> objs = new List<ObjectResult>();
            try
            {
                XmlDocument d = new XmlDocument();
                d.LoadXml(responseStr);

                XmlNodeList olist = d.GetElementsByTagName("Object");
                log.TraceEvent(TraceEventType.Verbose, 0, "Found " + olist.Count + " results");
                foreach (XmlNode xn in olist)
                {
                    ObjectResult obj = new ObjectResult();
                    // Get the objectId
                    obj.Id = new ObjectId(getChildByName("ObjectID", xn.ChildNodes).InnerText);

                    // Next, get the metadata
                    obj.meta = new MetadataList();
                    XmlNode sMetaNode = getChildByName("SystemMetadataList", xn.ChildNodes);
                    XmlNode uMetaNode = getChildByName("UserMetadataList", xn.ChildNodes);

                    if (sMetaNode != null)
                    {
                        foreach (XmlNode metaNode in sMetaNode.ChildNodes)
                        {
                            if (!metaNode.LocalName.Equals("Metadata"))
                            {
                                continue;
                            }

                            string mName = getChildByName("Name", metaNode.ChildNodes).InnerText;
                            string mValue = getChildByName("Value", metaNode.ChildNodes).InnerText;

                            obj.meta.AddMetadata(new Metadata(mName, mValue, false));
                        }
                    }

                    if (uMetaNode != null)
                    {
                        foreach (XmlNode metaNode in uMetaNode.ChildNodes)
                        {
                            if (!metaNode.LocalName.Equals("Metadata"))
                            {
                                continue;
                            }

                            string mName = getChildByName("Name", metaNode.ChildNodes).InnerText;
                            string mValue = getChildByName("Value", metaNode.ChildNodes).InnerText;
                            string mListable = getChildByName("Listable", metaNode.ChildNodes).InnerText;

                            obj.meta.AddMetadata(new Metadata(mName, mValue, "true".Equals(mListable)));
                        }
                    }

                    objs.Add(obj);
                }

            }
            catch (XmlException e)
            {
                throw new EsuException("Error parsing xml object list", e);
            }

            return objs;

        }

        private ServiceInformation parseServiceInformation(string responseStr, NameValueCollection headers)
        {
            try
            {
                XmlDocument d = new XmlDocument();
                d.LoadXml(responseStr);

                ServiceInformation si = new ServiceInformation();

                XmlNodeList olist = d.GetElementsByTagName("Atmos");
                log.TraceEvent(TraceEventType.Verbose, 0, "Found " + olist.Count + " results");
                foreach (XmlNode xn in olist)
                {
                    si.AtmosVersion = xn.InnerText;
                }

                foreach(string name in headers.AllKeys) {
                    if ("x-emc-support-utf8".Equals(name, StringComparison.OrdinalIgnoreCase) && "true".Equals(headers[name]))
                        si.UnicodeMetadataSupported = true;

                    else if ("x-emc-features".Equals(name, StringComparison.OrdinalIgnoreCase)) {
                        foreach (String feature in headers[name].Split(',')) {
                            si.AddFeature(feature);
                        }
                    }
                }

                return si;
            }
            catch (XmlException e)
            {
                throw new EsuException("Error parsing xml object list", e);
            }
        }



        private string getResourcePath(string ctx, Identifier id) {
		    if( id is ObjectId ) {
			    return ctx + "/objects/" + id;
		    } else if ( id is ObjectPath ) {
			    return ctx + "/namespace" + id;
            } else if (id is ObjectKey) {
                return ctx + "/namespace/" + (id as ObjectKey).key;
            } else {
                throw new Exception("Unknown identifier: " + id.GetType().Name);
            }
        }


        private List<ObjectId> getIds(List<ObjectResult> list)
        {
            List<ObjectId> ids = new List<ObjectId>(list.Count);
            foreach( ObjectResult result in list ) {
                ids.Add(result.Id);
            }
            return ids;
        }

        /// <summary>
        /// Joins a list of values with a delimiter
        /// </summary>
        /// <param name="list">The list to process</param>
        /// <param name="delim">The string to join the list elements with</param>
        /// <returns></returns>
        private string join(List<string> list, string delim)
        {
            bool first = true;
            StringBuilder sb = new StringBuilder();
            foreach (string val in list)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(delim);
                }
                sb.Append(val);
            }

            return sb.ToString();
        }

        private void writeRequestBodyFromStream(HttpWebRequest con, Stream data, long streamLength)
        {
            // post data
            Stream s = null;
            try
            {
                con.ContentLength = streamLength;
                con.SendChunked = false;
                byte[] buffer = new byte[64 * 1024];
                s = con.GetRequestStream();
                for (long i = 0; i < streamLength; )
                {
                    if (i + buffer.Length > streamLength)
                    {
                        int bytesToRead = (int)(streamLength - i);
                        int count = data.Read(buffer, 0, bytesToRead);
                        if (count == 0)
                        {
                            con.Abort();
                            throw new EsuException("Premature EOF reading stream at offset " + i);
                        }
                        s.Write(buffer, 0, count);
                        i += count;
                    }
                    else
                    {
                        int count = data.Read(buffer, 0, buffer.Length);
                        if (count == 0)
                        {
                            con.Abort();
                            throw new EsuException("Premature EOF reading stream at offset " + i);
                        }
                        s.Write(buffer, 0, count);
                        i += count;
                    }
                }
                s.Close();
            }
            catch (IOException e)
            {
                s.Close();
                throw new EsuException("Error posting data", e);
            }

        }

        private HttpWebRequest createWebRequest(Uri u)
        {
            HttpWebRequest con = (HttpWebRequest)WebRequest.Create(u);

            if(timeout != -1) 
            {
                con.Timeout = timeout;
            }
            if (readWriteTimeout != -1)
            {
                con.ReadWriteTimeout = readWriteTimeout;
            }

            if (proxy != null)
            {
                con.Proxy = proxy;
            }

            return con;
        }

        private void addDateHeader(Dictionary<string, string> headers)
        {
            DateTime dt = DateTime.Now.ToUniversalTime();
            dt = dt.AddSeconds(serverOffset);
            string dateHeader = dt.ToString("r");
            log.TraceEvent(TraceEventType.Verbose, 0, "Date: " + dateHeader);
            headers.Add("Date", dateHeader);
        }


        #endregion


    }
}
