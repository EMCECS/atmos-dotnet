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
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Reflection;
using System.Xml;

namespace EsuApiLib.Rest {
    /// <summary>
    /// Implements the REST version of the ESU API. This class uses 
    /// HttpWebRequest to perform object and metadata calls against 
    /// the ESU server. All of the methods that communicate with the 
    /// server are atomic and stateless so the object can be used 
    /// safely in a multithreaded environment. 
    /// </summary>
    public class EsuRestApi : EsuApi {
        private static readonly Regex OBJECTID_EXTRACTOR = new Regex( "/[0-9a-zA-Z]+/objects/([0-9a-f]{44})" );

        private byte[] secret;
        private string context = "/rest";

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

        private int port;

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

        private string protocol;

        /// <summary>
        /// The protocol used.  Generally is "http" or "https".
        /// </summary>
        public string Protocol {
            get { return protocol; }
            set { protocol = value; }
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
        public ObjectId CreateObject( Acl acl, MetadataList metadata, byte[] data, string mimeType ) {
            return CreateObjectFromSegment( acl, metadata,
                data == null ? new ArraySegment<byte>( new byte[0] ) : new ArraySegment<byte>( data ),
                mimeType );
        }

        /// <summary>
        /// Creates a new object in the cloud using an ArraySegment.
        /// </summary>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <returns>Identifier of the newly created object.</returns>
        public ObjectId CreateObjectFromSegment( Acl acl, MetadataList metadata, ArraySegment<byte> data, string mimeType ) {
            HttpWebResponse resp = null;
            try {
                string resource = context + "/objects";
                Uri u = buildUrl( resource );
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create( u );

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
                string dateHeader = DateTime.Now.ToUniversalTime().ToString( "r" );
                Debug.WriteLine( "Date: " + dateHeader );
                headers.Add( "Date", dateHeader );

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
                    Debug.WriteLine( "Id: " + id );
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
                byte[] data, String mimeType) {
            return CreateObjectFromSegmentOnPath(path, acl, metadata,
                data == null ? new ArraySegment<byte>(new byte[0]) : new ArraySegment<byte>(data),
                mimeType);
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
                ArraySegment<byte> data, String mimeType) {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath( context, path );
                Uri u = buildUrl(resource);
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create(u);

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
                string dateHeader = DateTime.Now.ToUniversalTime().ToString("r");
                Debug.WriteLine("Date: " + dateHeader);
                headers.Add("Date", dateHeader);

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
                    Debug.WriteLine("Id: " + id);
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
        /// Updates an object in the cloud.
        /// </summary>
        /// <param name="id">The ID of the object to update</param>
        /// <param name="acl">Access control list for the new object. Optional, set to NULL to leave the ACL unchanged.</param>
        /// <param name="metadata">Metadata list for the new object.  Optional, set to NULL for no changes to the metadata.</param>
        /// <param name="extent">portion of the object to update.  May be null to indicate the whole object is to be replaced.  If not null, the extent size must match the data size.</param>
        /// <param name="data">The new contents of the object.  May be appended to later. Optional, set to null for no content changes.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        public void UpdateObject( Identifier id, Acl acl, MetadataList metadata, Extent extent, byte[] data, string mimeType ) {
            ArraySegment<byte> seg = new ArraySegment<byte>( data == null ? new byte[0] : data );
            UpdateObjectFromSegment( id, acl, metadata, extent, seg, mimeType );
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
        public void UpdateObjectFromSegment( Identifier id, Acl acl, MetadataList metadata, Extent extent, ArraySegment<byte> data, string mimeType ) {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath(context, id);
                Uri u = buildUrl( resource );
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create( u );

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                // Figure out the mimetype
                if( mimeType == null ) {
                    mimeType = "application/octet-stream";
                }

                headers.Add( "Content-Type", mimeType );
                headers.Add( "x-emc-uid", uid );

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
                string dateHeader = DateTime.Now.ToUniversalTime().ToString( "r" );
                Debug.WriteLine( "Date: " + dateHeader );
                headers.Add( "Date", dateHeader );

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
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create( u );

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );

                // Add tags if needed
                if( tags != null ) {
                    processTags( tags, headers );
                }

                // Add date
                string dateHeader = DateTime.Now.ToUniversalTime().ToString( "r" );
                Debug.WriteLine( "Date: " + dateHeader );
                headers.Add( "Date", dateHeader );

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
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create( u );

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );

                // Add tags if needed
                if( tags != null ) {
                    processTags( tags, headers );
                }

                // Add date
                string dateHeader = DateTime.Now.ToUniversalTime().ToString( "r" );
                Debug.WriteLine( "Date: " + dateHeader );
                headers.Add( "Date", dateHeader );

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
        /// Reads an object's content.
        /// </summary>
        /// <param name="id">the identifier of the object whose content to read.</param>
        /// <param name="extent">the portion of the object data to read.  Optional.  If null, the entire object will be read.</param>
        /// <param name="buffer">the buffer to use to read the extent.  Must be large enough to read the response or an error will be thrown.  If null, a buffer will be allocated to hold the response data.  If you pass a buffer that is larger than the extent, only extent.getSize() bytes will be valid.</param>
        /// <returns>A byte array containing the requested content.</returns>
        public byte[] ReadObject( Identifier id, Extent extent, byte[] buffer ) {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath(context, id);
                Uri u = buildUrl( resource );
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create( u );

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );

                //Add extent if needed
                if( extent != null && !extent.Equals( Extent.ALL_CONTENT ) ) {
                    long end = extent.Offset + (extent.Size - 1);
                    headers.Add( "Range", "Bytes=" + extent.Offset + "-" + end );
                }

                // Add date
                string dateHeader = DateTime.Now.ToUniversalTime().ToString( "r" );
                Debug.WriteLine( "Date: " + dateHeader );
                headers.Add( "Date", dateHeader );

                // Sign request
                signRequest( con, "GET", resource, headers );

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

                byte[] responseBuffer = readResponse( resp, buffer );
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
        /// Deletes an object from the cloud.
        /// </summary>
        /// <param name="id">The identifier of the object to delete.</param>
        public void DeleteObject( Identifier id ) {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath(context, id);
                Uri u = buildUrl( resource );
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create( u );

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );

                // Add date
                string dateHeader = DateTime.Now.ToUniversalTime().ToString( "r" );
                Debug.WriteLine( "Date: " + dateHeader );
                headers.Add( "Date", dateHeader );

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
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create( u );

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );

                // Add date
                string dateHeader = DateTime.Now.ToUniversalTime().ToString( "r" );
                Debug.WriteLine( "Date: " + dateHeader );
                headers.Add( "Date", dateHeader );

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
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create( u );

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );

                // Add acl
                if( acl != null ) {
                    processAcl( acl, headers );
                }

                // Add date
                string dateHeader = DateTime.Now.ToUniversalTime().ToString( "r" );
                Debug.WriteLine( "Date: " + dateHeader );
                headers.Add( "Date", dateHeader );

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
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create( u );

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );

                // Process metadata
                if( metadata != null ) {
                    processMetadata( metadata, headers );
                }

                // Add date
                string dateHeader = DateTime.Now.ToUniversalTime().ToString( "r" );
                Debug.WriteLine( "Date: " + dateHeader );
                headers.Add( "Date", dateHeader );

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
                string resource = getResourcePath(context, id) + "/metadata/user";
                Uri u = buildUrl( resource );
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create( u );

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );

                // Add tags if needed
                processTags( tags, headers );


                // Add date
                string dateHeader = DateTime.Now.ToUniversalTime().ToString( "r" );
                Debug.WriteLine( "Date: " + dateHeader );
                headers.Add( "Date", dateHeader );

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
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create( u );

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );


                // Add date
                string dateHeader = DateTime.Now.ToUniversalTime().ToString( "r" );
                Debug.WriteLine( "Date: " + dateHeader );
                headers.Add( "Date", dateHeader );

                // Sign request
                signRequest( con, "GET", resource, headers );

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

                // Get object id list from response
                byte[] response = readResponse( resp, null );

                string responseStr = Encoding.UTF8.GetString( response );
                Debug.WriteLine( "Response: " + responseStr );

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
        /// Creates a new immutable version of an object.
        /// </summary>
        /// <param name="id">The object to version</param>
        /// <returns>The id of the newly created version</returns>
        public ObjectId VersionObject( Identifier id ) {
            HttpWebResponse resp = null;
            try {
                string resource = getResourcePath(context, id) + "?versions";
                Uri u = buildUrl( resource );
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create( u );

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );

                // Add date
                string dateHeader = DateTime.Now.ToUniversalTime().ToString( "r" );
                Debug.WriteLine( "Date: " + dateHeader );
                headers.Add( "Date", dateHeader );

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
                    Debug.WriteLine( "Id: " + vid );
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
        /// <returns>The list of objects with the given tag.  If no objects are found the array will be empty.</returns>
        /// <exception cref="T:EsuApiLib.EsuException">if no objects are found (code 1003)</exception>
        public List<ObjectId> ListObjects( string tag ) {
            HttpWebResponse resp = null;
            try {
                string resource = context + "/objects";
                Uri u = buildUrl( resource );
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create( u );

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );

                // Add tag
                if( tag != null ) {
                    headers.Add( "x-emc-tags", tag );
                } else {
                    throw new EsuException( "tag may not be null" );
                }

                // Add date
                string dateHeader = DateTime.Now.ToUniversalTime().ToString( "r" );
                Debug.WriteLine( "Date: " + dateHeader );
                headers.Add( "Date", dateHeader );

                // Sign request
                signRequest( con, "GET", resource, headers );

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

                // Get object id list from response
                byte[] response = readResponse( resp, null );

                string responseStr = Encoding.UTF8.GetString( response );
                Debug.WriteLine( "Response: " + responseStr );

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
            HttpWebResponse resp = null;
            try
            {
                string resource = context + "/objects";
                Uri u = buildUrl(resource);
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add("x-emc-uid", uid);
                headers.Add("x-emc-include-meta", "1");

                // Add tag
                if (tag != null)
                {
                    headers.Add("x-emc-tags", tag);
                }
                else
                {
                    throw new EsuException("tag may not be null");
                }

                // Add date
                string dateHeader = DateTime.Now.ToUniversalTime().ToString("r");
                Debug.WriteLine("Date: " + dateHeader);
                headers.Add("Date", dateHeader);

                // Sign request
                signRequest(con, "GET", resource, headers);

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if (statInt > 299)
                {
                    handleError(resp);
                }

                // Get object id list from response
                byte[] response = readResponse(resp, null);

                string responseStr = Encoding.UTF8.GetString(response);
                Debug.WriteLine("Response: " + responseStr);

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
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create( u );

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );

                // Add tag
                if( tag != null ) {
                    headers.Add( "x-emc-tags", tag );
                }

                // Add date
                string dateHeader = DateTime.Now.ToUniversalTime().ToString( "r" );
                Debug.WriteLine( "Date: " + dateHeader );
                headers.Add( "Date", dateHeader );

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
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create( u );

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add( "x-emc-uid", uid );

                // Add date
                string dateHeader = DateTime.Now.ToUniversalTime().ToString( "r" );
                Debug.WriteLine( "Date: " + dateHeader );
                headers.Add( "Date", dateHeader );

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
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create( u );

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
                string dateHeader = DateTime.Now.ToUniversalTime().ToString( "r" );
                Debug.WriteLine( "Date: " + dateHeader );
                headers.Add( "Date", dateHeader );

                // Sign request
                signRequest( con, "GET", resource, headers );

                // Check response
                resp = (HttpWebResponse)con.GetResponse();
                int statInt = (int)resp.StatusCode;
                if( statInt > 299 ) {
                    handleError( resp );
                }

                // Get object id list from response
                byte[] response = readResponse( resp, null );

                string responseStr = Encoding.UTF8.GetString( response );
                Debug.WriteLine( "Response: " + responseStr );

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
        public List<DirectoryEntry> ListDirectory( ObjectPath path ) {
            if (!path.IsDirectory()) {
                throw new EsuException("listDirectory must be called with a directory path");
            }

            // Read out the directory's contents
            byte[] dir = ReadObject(path, null, null);

            XmlDocument d = new XmlDocument();
            d.LoadXml( Encoding.UTF8.GetString( dir ) );

            List<DirectoryEntry> entries = new List<DirectoryEntry>();
            Debug.WriteLine(Encoding.UTF8.GetString(dir));
            XmlNodeList olist = d.GetElementsByTagName("DirectoryEntry");
            Debug.WriteLine("Found " + olist.Count + " objects in directory");
            foreach (XmlNode xn in olist) {
                DirectoryEntry de = new DirectoryEntry();
                string name = null;
                foreach( XmlNode child in xn.ChildNodes ) {
                    if( child.LocalName.Equals( "ObjectID" ) ) {
                        de.Id = new ObjectId( child.InnerText );
                    } else if( child.LocalName.Equals( "Filename" ) ) {
                        name = child.InnerText;
                    } else if( child.LocalName.Equals( "FileType" ) ) {
                        de.Type = child.InnerText;
                    }
                }

                if( name == null ) {
                    throw new EsuException( "Could not find object name in directory!" );
                }
                if( "directory".Equals( de.Type ) ) {
                    name += "/";
                }

                de.Path = new ObjectPath( path.ToString() + name );
                entries.Add( de );

            }

            return entries;
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
                HttpWebRequest con = (HttpWebRequest)WebRequest.Create(u);

                // Build headers
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers.Add("x-emc-uid", uid);

                // Add date
                string dateHeader = DateTime.Now.ToUniversalTime().ToString("r");
                Debug.WriteLine("Date: " + dateHeader);
                headers.Add("Date", dateHeader);

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
        public Uri getShareableUrl(Identifier id, DateTime expiration) {
            string resource = getResourcePath(context, id);
            string uidEnc = Uri.EscapeDataString(uid);
            string unixTime = (expiration - new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)).TotalSeconds.ToString( "F0" );

            StringBuilder sb = new StringBuilder();
            sb.Append("GET\n");
            sb.Append(resource.ToLower() + "\n");
            sb.Append(uidEnc + "\n");

            sb.Append("" + unixTime);

            string signature = Uri.EscapeDataString(sign(sb.ToString()));
            resource += "?uid=" + uid + "&expires=" + unixTime +
                "&signature=" + signature;

            Uri u = buildUrl(resource);

            return u;

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

        #endregion

        #region PrivateMethods
        /**
         * Builds a new URL to the given resource
         */
        private Uri buildUrl( string resource ) {
            return new Uri( protocol + "://" + host + ":" + port + resource );
        }

        private void processMetadata( MetadataList metadata, Dictionary<string, string> headers ) {
            StringBuilder listable = new StringBuilder();
            StringBuilder nonListable = new StringBuilder();

            Debug.WriteLine( "Processing " + metadata.Count() + " metadata entries" );

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

            // Only set the headers if there's data
            if( listable.Length > 0 ) {
                headers.Add( "x-emc-listable-meta", listable.ToString() );
            }
            if( nonListable.Length > 0 ) {
                headers.Add( "x-emc-meta", nonListable.ToString() );
            }

        }

        private string formatTag( Metadata meta ) {
            // strip commas and newlines for now.
            string s = meta.Value.Replace( ",", "" );
            s = s.Replace( "\n", "" );
            return meta.Name + "=" + s;
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
            hashStr.Append( method + "\n" );

            // If content type exists, add it.  Otherwise add a blank line.
            if( headers.ContainsKey( "Content-Type" ) ) {
                Debug.WriteLine( "Content-Type: " + headers["Content-Type"] );
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
            hashStr.Append( headers["Date"] + "\n" +
                    resource.ToLower() + "\n" );

            // Do the 'x-emc' headers.  The headers must be hashed in alphabetic
            // order and the values must be stripped of whitespace and newlines.
            List<string> keys = new List<string>();
            Dictionary<string, string> newheaders = new Dictionary<string, string>();

            // Extract the keys and values
            foreach( string key in headers.Keys ) {
                if( key.IndexOf( "x-emc" ) == 0 ) {
                    keys.Add( key.ToLower() );
                    newheaders.Add( key.ToLower(), headers[key].Replace( "\n", "" ) );
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

            string hashOut = sign(hashStr.ToString());

            // Can set all the headers, etc now.  Microsoft doesn't let you
            // set some of the headers directly.  Modify the headers through
            // reflection to get around this.
            MethodInfo m = con.Headers.GetType().GetMethod( "AddWithoutValidate", BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Instance, null,
                new Type[] { typeof( string ), typeof( string ) }, null );

            foreach( string name in headers.Keys ) {
                Debug.WriteLine( "Setting " + name );
                m.Invoke( con.Headers, new object[] { name, headers[name] } );
            }

            // Set the signature header
            con.Headers["x-emc-signature"] = hashOut;

            // Set the method.
            con.Method = method;

        }

        private string sign(string hashStr) {
            Debug.WriteLine("Hashing: \n" + hashStr);

            // Compute the signature hash
            HMACSHA1 mac = new HMACSHA1(secret);
            byte[] hashBytes = Encoding.UTF8.GetBytes(hashStr.ToString());
            Debug.WriteLine(hashBytes.Length + " bytes to hash");
            mac.TransformFinalBlock(hashBytes, 0, hashBytes.Length);
            byte[] hashData = mac.Hash;
            Debug.WriteLine(hashData.Length + " bytes in signature");
            // Encode the hash in Base64.
            string hashOut = Convert.ToBase64String(hashData);

            Debug.WriteLine("Hash: " + hashOut);
            return hashOut;
        }

        private void handleError( HttpWebResponse resp ) {
            // Try and read the response body.
            try {
                byte[] response = readResponse( resp, null );
                string responseText = Encoding.UTF8.GetString( response );
                Debug.WriteLine( "Error response: " + responseText );
                XmlDocument d = new XmlDocument();
                d.LoadXml( responseText );

                if( d.GetElementsByTagName( "Code" ).Count == 0 ||
                    d.GetElementsByTagName( "Message" ).Count == 0 ) {
                    // not an error from ESU
                    throw new EsuException( resp.StatusDescription, (int)resp.StatusCode );
                }

                string code = d.GetElementsByTagName( "Code" ).Item( 0 ).InnerText;
                string message = d.GetElementsByTagName( "Message" ).Item( 0 ).InnerText;


                Debug.WriteLine( "Error: " + code + " message: " + message );
                throw new EsuException( message, int.Parse( code ) );

            } catch( IOException e ) {
                Debug.WriteLine( "Could not read error response body: " + e );
                // Just throw what we know from the response
                try {
                    throw new EsuException( resp.StatusDescription, (int)resp.StatusCode );
                } catch( IOException e1 ) {
                    Debug.WriteLine( "Could not get response code/message: " + e1 );
                    throw new EsuException( "Could not get response code", e1 );
                }
            } catch( XmlException e ) {
                try {
                    Debug.WriteLine( "Could not parse response body for " +
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

        private byte[] readResponse( HttpWebResponse resp, byte[] buffer ) {
            Stream rs = resp.GetResponseStream();
            try {
                byte[] output;
                int contentLength = (int)resp.ContentLength;
                // If we know the content length, read it directly into a buffer.
                if( contentLength != -1 ) {
                    if( buffer != null && buffer.Length < contentLength ) {
                        throw new EsuException( "The response buffer was not long enough to hold the response: " + buffer.Length + "<" + contentLength );
                    }
                    if( buffer != null ) {
                        output = buffer;
                    } else {
                        output = new byte[(int)resp.ContentLength];
                    }

                    int c = 0;
                    while( c < contentLength ) {
                        int read = rs.Read( output, c, contentLength - c );
                        if( read == 0 ) {
                            // EOF!
                            throw new EsuException( "EOF reading response at position " + c + " size " + (contentLength - c) );
                        }
                        c += read;
                    }

                    return output;
                } else {
                    Debug.WriteLine( "Content length is unknown.  Buffering output." );
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

            Debug.WriteLine( "Processing " + tags.Count() + " metadata tag entries" );

            foreach( MetadataTag tag in tags ) {
                if( taglist.Length > 0 ) {
                    taglist.Append( "," );
                }
                taglist.Append( tag.Name );
            }

            if( taglist.Length > 0 ) {
                headers.Add( "x-emc-tags", taglist.ToString() );
            }
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

                Metadata m = new Metadata( name, value, listable );
                Debug.WriteLine( "Meta: " + m );
                meta.AddMetadata( m );
            }

        }


        private void readAcl( Acl acl, string header, EsuApiLib.Grantee.GRANTEE_TYPE type ) {
            Debug.WriteLine( "readAcl: " + header );
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

                Debug.WriteLine( "grant: " + grantee + "." + permission + " (" + type
                        + ")" );

                Grantee ge = new Grantee( grantee, type );
                Grant gr = new Grant( ge, permission );
                Debug.WriteLine( "Grant: " + gr );
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
                tags.AddTag( new MetadataTag( attr, listable ) );
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
                Debug.WriteLine("Found " + olist.Count + " results");
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

        private List<ObjectResult> parseObjectListWithMetadata(string responseStr)
        {
            List<ObjectResult> objs = new List<ObjectResult>();
            try
            {
                XmlDocument d = new XmlDocument();
                d.LoadXml(responseStr);

                XmlNodeList olist = d.GetElementsByTagName("Object");
                Debug.WriteLine("Found " + olist.Count + " results");
                foreach (XmlNode xn in olist)
                {
                    ObjectResult obj = new ObjectResult();
                    // Get the objectId
                    obj.Id = new ObjectId(getChildByName("ObjectID", xn.ChildNodes).InnerText);

                    // Next, get the metadata
                    obj.meta = new MetadataList();
                    XmlNodeList sMeta = getChildByName("SystemMetadataList", xn.ChildNodes).ChildNodes;
                    XmlNodeList uMeta = getChildByName("UserMetadataList", xn.ChildNodes).ChildNodes;

                    foreach (XmlNode metaNode in sMeta)
                    {
                        if (!metaNode.LocalName.Equals("Metadata"))
                        {
                            continue;
                        }

                        string mName = getChildByName("Name", metaNode.ChildNodes).InnerText;
                        string mValue = getChildByName("Value", metaNode.ChildNodes).InnerText;

                        obj.meta.AddMetadata(new Metadata(mName, mValue, false));
                    }

                    foreach (XmlNode metaNode in uMeta)
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


                    objs.Add(obj);
                }

            }
            catch (XmlException e)
            {
                throw new EsuException("Error parsing xml object list", e);
            }

            return objs;

        }

        private string getResourcePath(string ctx, Identifier id) {
		    if( id is ObjectId ) {
			    return ctx + "/objects/" + id;
		    } else {
			    return ctx + "/namespace" + id;
		    }
        }


        #endregion


    }
}
