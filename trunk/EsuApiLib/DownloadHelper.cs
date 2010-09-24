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
using System.Diagnostics;

namespace EsuApiLib {
    /// <summary>
    /// Helper class to download objects. For large transfers, the content 
    /// generally needs to be transferred from the server in smaller chunks. 
    /// This helper class reads an object's contents incrementally from the 
    /// server and writes it to a file or stream. 
    /// </summary>
    public class DownloadHelper {
        private const int DEFAULT_BUFFSIZE = 4194304; // 4MB

        private EsuApi esu;
        private ArraySegment<byte> buffer;
        private bool closeStream;
        private Stream stream;
        private Checksum checksum;

        /// <summary>
        /// If true, this object will verify the checksum of the 
        /// object after download.
        /// </summary>
        public bool Checksumming { set; get; }

        private long currentBytes;

        /// <summary>
        /// The number of bytes that have been transferred
        /// </summary>
        public long CurrentBytes {
            get { return currentBytes; }
        }

        private long totalBytes;

        /// <summary>
        /// The total number of bytes to be transferred.
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

        private Exception error;

        /// <summary>
        /// The error that caused the transfer to fail.  Will
        /// be null if the transfer has not failed.
        /// </summary>
        public Exception Error {
            get { return error; }
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
        /// Creates a new download helper.
        /// </summary>
        /// <param name="esuApi">the API connection object to use to communicate
        /// with the server.</param>
        /// <param name="buffer">the buffer to use for the transfers from the server.  If
        /// null, a default 4MB buffer will be used.</param>
        public DownloadHelper( EsuApi esuApi, byte[] buffer ) {
            this.esu = esuApi;
            this.buffer = new ArraySegment<byte>(
                    buffer == null ? new byte[DEFAULT_BUFFSIZE] : buffer );
        }

        /// <summary>
        /// Downloads the given object's contents to a file.
        /// </summary>
        /// <param name="id">the identifier of the object to download</param>
        /// <param name="file">the file to write the object's contents to.</param>
        public void ReadObject( ObjectId id, string file ) {
            Stream s;
            try {
                s = File.OpenWrite( file );
            } catch ( FileNotFoundException e ) {
                throw new EsuException( "Error opening output file", e );
            }
            ReadObject( id, s, true );
        }

        /// <summary>
        /// Downloads the given object's contents to a stream.
        /// </summary>
        /// <param name="id">the identifier of the object to download.</param>
        /// <param name="stream">the stream to write the object's contents to.</param>
        /// <param name="closeStream">specifies whether to close the stream after
        /// the transfer is complete.</param>
        public void ReadObject( ObjectId id, Stream stream, bool closeStream ) {

            this.currentBytes = 0;
            this.complete = false;
            this.failed = false;
            this.error = null;
            this.closeStream = closeStream;
            this.stream = stream;

            if (Checksumming)
            {
                checksum = new Checksum(Checksum.Algorithm.SHA0);
            }

            // Get the file size.  Set to -1 if unknown.
            MetadataList sMeta = this.esu.GetSystemMetadata( id, null );
            if ( sMeta.GetMetadata( "size" ) != null ) {
                string size = sMeta.GetMetadata( "size" ).Value;
                if ( size != null && size.Length > 0 ) {
                    this.totalBytes = long.Parse( size );
                } else {
                    this.totalBytes = -1;
                }
            } else {
                this.totalBytes = -1;
            }

            // We need to know how big the object is to download it.  Fail the
            // transfer if we can't determine the object size.
            if ( this.totalBytes == -1 ) {
                throw new EsuException( "Failed to get object size" );
            }

            // Loop, downloading chunks until the transfer is complete.
            while ( true ) {
                try {
                    Extent extent = null;

                    // Determine how much data to download.  If we're at the last
                    // request in the transfer, only request as many bytes as needed
                    // to get to the end of the file.  Use bcmath since these values
                    // can easily exceed 2GB.
                    if ( currentBytes + buffer.Array.Length > totalBytes ) {
                        // Would go past end of file.  Request less bytes.                                      
                        extent = new Extent( this.currentBytes, totalBytes
                                - currentBytes );
                    } else {
                        extent = new Extent( this.currentBytes,
                                buffer.Array.Length );
                    }
                    if ( extent.Size != buffer.Count ) {
                        buffer = new ArraySegment<byte>( buffer.Array, 0, (int)extent.Size );
                    }

                    // Read data from the server.
                    byte[] responseBuffer = this.esu.ReadObject( id, extent, buffer.Array, checksum );

                    // Write to the stream
                    stream.Write( responseBuffer, 0, (int)extent.Size );

                    // Update progress
                    this.OnProgress( buffer.Count );

                    // See if we're done.
                    if ( this.currentBytes == this.totalBytes ) {

                        if (Checksumming && checksum != null && checksum.ExpectedValue != null)
                        {
                            if (!checksum.ExpectedValue.Equals(checksum.ToString()))
                            {
                                throw new EsuException("Checksum validation failed.  Expected " + checksum.ExpectedValue + " but computed " + checksum.ToString());
                            }
                            Debug.WriteLine("Checksum OK: " + checksum.ExpectedValue);
                        }

                        this.OnComplete();
                        return;
                    }
                } catch ( EsuException e ) {
                    OnFail( e );
                    throw e;
                } catch ( IOException e ) {
                    OnFail( e );
                    throw new EsuException( "Error downloading file", e );
                }
            }
        }

        #endregion


        #region Private Methods

        /// <summary>
        /// Updates progress on the current transfer and fires a progress event
        /// </summary>
        /// <param name="bytes">the number of bytes transferred</param>
        private void OnProgress( long bytes ) {
            this.currentBytes += bytes;

            if ( ProgressEvent != null ) {
                ProgressEvent( this, new ProgressEventArgs( currentBytes, totalBytes ) );
            }
        }

        /// <summary>
        /// Marks the current transfer as complete, closes the stream if required,
        /// and fires a complete event
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
        /// Fails the current transfer.  Sets the failed flag and fires a failure event
        /// </summary>
        /// <param name="e">the error that caused the transfer to fail.</param>
        private void OnFail( Exception e ) {
            this.failed = true;
            this.error = e;

            if ( FailureEvent != null ) {
                FailureEvent( this, new FailureEventArgs( e ) );
            }

        }
        #endregion

    }
}
