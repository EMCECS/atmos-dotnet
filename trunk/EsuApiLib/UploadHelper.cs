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
using System.IO;

namespace EsuApiLib {
    /// <summary>
    /// Helper class to create and update objects. For large transfers, the content 
    /// generally needs to be transferred to the server in smaller chunks. This class 
    /// reads data from either a file or a stream and incrementally uploads it to the 
    /// server. The class also supports the registering of a listener object to report 
    /// status back to the calling application.
    /// </summary>
    public class UploadHelper {
        private const int DEFAULT_BUFFSIZE = 4096 * 1024; // 4MB

        private ArraySegment<byte> buffer;
        private EsuApi esu;
        private bool closeStream;
        private Stream stream;

        private Exception error;

        /// <summary>
        /// The error that caused the transfer to fail.  Will
        /// be null if the transfer has not failed.
        /// </summary>
        public Exception Error {
            get { return error; }
        }

        private long currentBytes;

        /// <summary>
        /// The number of bytes that have been transferred
        /// </summary>
        public long CurrentBytes {
            get { return currentBytes; }
        }

        private long totalBytes;

        /// <summary>
        /// The total number of bytes to be transferred.  If unknown,
        /// it will be set to -1.
        /// </summary>
        public long TotalBytes {
            get { return totalBytes; }
            set { totalBytes = value; }
        }

        private bool complete;

        /// <summary>
        /// Set to true if the transfer has completed.
        /// </summary>
        public bool Complete {
            get { return complete; }
        }

        private bool failed;

        /// <summary>
        /// Set to true if the transfer has failed.
        /// </summary>
        public bool Failed {
            get { return failed; }
        }
	

        #region Events
        /// <summary>
        /// Progress event delegates are used for handling progress events.
        /// </summary>
        /// <param name="sender">The UploadHelper that fired the event</param>
        /// <param name="e">The progress information</param>
        public delegate void ProgressEventDelegate( object sender, ProgressEventArgs e );
        /// <summary>
        /// Progress events are fired after each chunk has transferred to update
        /// progress information.
        /// </summary>
        public event ProgressEventDelegate ProgressEvent;
        /// <summary>
        /// Complete event delegates are used for handling complete events.
        /// </summary>
        /// <param name="sender">The UploadHelper that fired the event</param>
        /// <param name="e">The complete event (empty)</param>
        public delegate void CompleteEventDelegate( object sender, EventArgs e );
        /// <summary>
        /// The complete event is fired when a trasfer has completed.
        /// </summary>
        public event CompleteEventDelegate CompleteEvent;
        /// <summary>
        /// Failure event delegates are used for handling failure events.
        /// </summary>
        /// <param name="sender">The UploadHelper that fired the event</param>
        /// <param name="e">The failure event information</param>
        public delegate void FailureEventDelegate( object sender, FailureEventArgs e );
        /// <summary>
        /// the failure event is fired when a transfer fails.
        /// </summary>
        public event FailureEventDelegate FailureEvent;
        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a new upload helper.
        /// </summary>
        /// <param name="esu">the API connection object to use to communicate
        /// with the server</param>
        /// <param name="buffer">the buffer used for making the transfers.  If null, a
        /// 4MB buffer will be allocated.</param>
        public UploadHelper( EsuApi esu, byte[] buffer ) {
            this.esu = esu;
            if ( buffer == null ) {
                this.buffer = new ArraySegment<byte>( new byte[DEFAULT_BUFFSIZE] );
            } else {
                this.buffer = new ArraySegment<byte>( buffer );
            }
        }

        /// <summary>
        /// Creates a new upload helper using a default 4MB buffer.
        /// </summary>
        /// <param name="api">the API connection object to use to communicate
        /// with the server</param>
        public UploadHelper( EsuApi api )
            : this( api, null ) {
        }

        /// <summary>
        /// Creates a new object on the server with the contents of the given file,
        /// acl and metadata.
        /// </summary>
        /// <param name="file">the path to the file to upload</param>
        /// <param name="acl">the ACL to assign to the new object.  Optional.  If null,
        /// the server will generate a default ACL for the file.</param>
        /// <param name="meta">The metadata to assign to the new object.
        /// Optional.  If null, no user metadata will be assigned to the new object.</param>
        /// <returns>the identifier of the newly-created object.</returns>
        public ObjectId CreateObject( string file, Acl acl, MetadataList meta ) {
            Stream s;
            // Open the file and call the streaming version
            try {
                totalBytes = new FileInfo( file ).Length;
                s = File.OpenRead( file );
            } catch ( FileNotFoundException e ) {
                throw new EsuException( "Could not open input file", e );
            }
            return CreateObject( s, acl, meta, true );
        }

        /// <summary>
        /// Creates a new object on the server with the contents of the given stream,
        /// acl and metadata.
        /// </summary>
        /// <param name="stream">the stream to upload.  The stream will be read until
        /// an EOF is encountered.</param>
        /// <param name="acl">the ACL to assign to the new object.  Optional.  If null,
        /// the server will generate a default ACL for the file.</param>
        /// <param name="metadata">The metadata to assign to the new object.
        /// Optional.  If null, no user metadata will be assigned to the new object.</param>
        /// <param name="closeStream">if true, the stream will be closed after
        /// the transfer completes.  If false, the stream will not be closed.</param>
        /// <returns>the identifier of the newly-created object.</returns>
        public ObjectId CreateObject( Stream stream, Acl acl,
                MetadataList metadata, bool closeStream ) {

            this.currentBytes = 0;
            this.complete = false;
            this.failed = false;
            this.error = null;
            this.closeStream = closeStream;
            this.stream = stream;

            ObjectId id = null;

            // First call should be to create object
            try {
                bool eof = ReadChunk();
                id = this.esu.CreateObjectFromSegment( acl, metadata, buffer, null );
                if ( !eof ) {
                    this.OnProgress( buffer.Count );
                } else {
                    // No data in file? Complete
                    this.OnComplete();
                    return id;
                }

                // Continue appending
                this.AppendChunks( id );

            } catch ( EsuException e ) {
                this.OnFail( e );
                throw e;
            } catch ( IOException e ) {
                this.OnFail( e );
                throw new EsuException( "Error uploading object", e );
            }

            return id;
        }

        /// <summary>
        /// Updates an existing object with the contents of the given file, ACL, and
        /// metadata.
        /// </summary>
        /// <param name="id">the identifier of the object to update.</param>
        /// <param name="file">the path to the file to replace the object's current
        /// contents with</param>
        /// <param name="acl">the ACL to update the object with.  Optional.  If null,
        /// the ACL will not be modified.</param>
        /// <param name="metadata">The metadata to assign to the object.
        /// Optional.  If null, no user metadata will be modified.</param>
        public void UpdateObject( ObjectId id, string file, Acl acl, MetadataList metadata ) {
            // Open the file and call the streaming version
            Stream s;
            try {
                s = File.OpenRead( file );
            } catch ( FileNotFoundException e ) {
                throw new EsuException( "Could not open input file", e );
            }
            totalBytes = new FileInfo( file ).Length;
            UpdateObject( id, s, acl, metadata, true );
        }

        /// <summary>
        /// Updates an existing object with the contents of the given stream, ACL, and
        /// metadata.
        /// </summary>
        /// <param name="id">the identifier of the object to update.</param>
        /// <param name="stream">the stream to replace the object's current
        /// contents with.  The stream will be read until an EOF is encountered.</param>
        /// <param name="acl">the ACL to update the object with.  Optional.  If not
        /// specified, the ACL will not be modified.</param>
        /// <param name="metadata">The metadata to assign to the object.
        /// Optional.  If null, no user metadata will be modified.</param>
        /// <param name="closeStream">If true, the stream will be closed after
        /// the object is updated.</param>
        public void UpdateObject( ObjectId id, Stream stream, Acl acl,
                MetadataList metadata, bool closeStream ) {

            this.currentBytes = 0;
            this.complete = false;
            this.failed = false;
            this.error = null;
            this.closeStream = closeStream;
            this.stream = stream;

            // First call uses a null extent to truncate the file.
            try {
                bool eof = ReadChunk();
                this.esu.UpdateObjectFromSegment( id, acl, metadata, null, buffer,
                        null );

                if ( !eof ) {
                    this.OnProgress( buffer.Count );
                } else {
                    // No data in file? Complete
                    this.OnComplete();
                    return;
                }

                // Continue appending
                this.AppendChunks( id );

            } catch ( EsuException e ) {
                this.OnFail( e );
                throw e;
            } catch ( IOException e ) {
                this.OnFail( e );
                throw new EsuException( "Error updating object", e );
            }

        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Continues writing data to the object until EOF
        /// </summary>
        private void AppendChunks( ObjectId id ) {
            while ( true ) {
                bool eof = ReadChunk();
                if ( eof ) {
                    // done
                    OnComplete();
                    return;
                }

                Extent extent = new Extent( currentBytes, buffer.Count );
                esu.UpdateObjectFromSegment( id, null, null, extent, buffer, null );
                this.OnProgress( buffer.Count );
            }

        }

        /// <summary>
        /// Fails the upload and notifies the listeners.
        /// </summary>
        /// <param name="e">exception that caused the failure.</param>
        private void OnFail( Exception e ) {
            failed = true;
            error = e;
            if ( closeStream ) {
                stream.Close();
            }

            if ( FailureEvent != null ) {
                FailureEvent( this, new FailureEventArgs( e ) );
            }
        }

        /// <summary>
        /// Marks the upload as completed and notifies the listeners.
        /// </summary>
        private void OnComplete() {
            complete = true;

            if ( closeStream ) {
                stream.Close();
            }

            if ( CompleteEvent != null ) {
                CompleteEvent( this, new EventArgs() );
            }
        }

        /// <summary>
        /// Notifies the listeners of upload progress.
        /// </summary>
        /// <param name="count">The number of bytes transferred</param>
        private void OnProgress( int count ) {
            currentBytes += count;

            if ( ProgressEvent != null ) {
                ProgressEvent( this, new ProgressEventArgs( currentBytes, totalBytes ) );
            }

        }

        /// <summary>
        /// Reads a chunk of data from the stream.
        /// </summary>
        /// <returns>true if an EOF was encountered.</returns>
        private bool ReadChunk() {
            int c = stream.Read( buffer.Array, 0, buffer.Count );
            buffer = new ArraySegment<byte>( buffer.Array, 0, c );
            if ( c == 0 ) {
                return true;
            }
            return false;
        }

        #endregion
    }
}
