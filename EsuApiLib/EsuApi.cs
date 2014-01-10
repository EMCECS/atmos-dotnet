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
using System.Text;
using System.IO;
using EsuApiLib.Multipart;

namespace EsuApiLib
{
    /// <summary>
    /// This interface defines the basic operations available through the ESU web
    /// services.
    /// </summary>
    public interface EsuApi
    {
        /// <summary>
        /// Creates a new object in the cloud.
        /// </summary>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <returns>Identifier of the newly created object.</returns>
        ObjectId CreateObject(Acl acl, MetadataList metadata,
                byte[] data, string mimeType);

        /// <summary>
        /// Creates a new object in the cloud.
        /// </summary>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <param name="checksum">the checksum object to use to compute checksums.  If you're doing incremental updates after the create, include the same object in subsequent calls.  Can be null to omit checksums.</param>
        /// <returns>Identifier of the newly created object.</returns>
        ObjectId CreateObject(Acl acl, MetadataList metadata,
                byte[] data, string mimeType, Checksum checksum);

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
        ObjectId CreateObjectOnPath(ObjectPath path, Acl acl,
                MetadataList metadata,
                byte[] data, String mimeType);

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
        ObjectId CreateObjectOnPath(ObjectPath path, Acl acl,
                MetadataList metadata,
                byte[] data, String mimeType, Checksum checksum);

        /// <summary>
        /// Creates a new object in the cloud using an ArraySegment.
        /// </summary>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <returns>Identifier of the newly created object.</returns>
        ObjectId CreateObjectFromSegment(Acl acl, MetadataList metadata,
                ArraySegment<byte> data, string mimeType);

        /// <summary>
        /// Creates a new object in the cloud using an ArraySegment.
        /// </summary>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <param name="checksum">the checksum object to use to compute checksums.  If you're doing incremental updates after the create, include the same object in subsequent calls.  Can be null to omit checksums.</param>
        /// <returns>Identifier of the newly created object.</returns>
        ObjectId CreateObjectFromSegment(Acl acl, MetadataList metadata,
                ArraySegment<byte> data, string mimeType, Checksum checksum);

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
        ObjectId CreateObjectFromStream(Acl acl, MetadataList metadata, Stream data, long streamLength, string mimeType, Checksum checksum);


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
        ObjectId CreateObjectFromStreamOnPath(ObjectPath path, Acl acl, MetadataList metadata, Stream data, long streamLength, string mimeType, Checksum checksum);


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
        ObjectId CreateObjectFromSegmentOnPath(ObjectPath path,
                Acl acl, MetadataList metadata,
                ArraySegment<byte> data, String mimeType);

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
        ObjectId CreateObjectFromSegmentOnPath(ObjectPath path,
                Acl acl, MetadataList metadata,
                ArraySegment<byte> data, String mimeType, Checksum checksum);

        /// <summary>
        /// Creates a new object in the cloud with the given key (and key-pool).
        /// </summary>
        /// <param name="key">the key-pool and key to use for the new object.</param>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content or a directory.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <returns>the ObjectId of the newly-created object for references by ID.</returns>
        ObjectId CreateObjectWithKey(ObjectKey key, Acl acl, MetadataList metadata, byte[] data,
                String mimeType);

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
        ObjectId CreateObjectWithKey(ObjectKey key, Acl acl, MetadataList metadata, byte[] data,
                String mimeType, Checksum checksum);

        /// <summary>
        /// Creates a new object in the cloud with the given key (and key-pool).
        /// </summary>
        /// <param name="key">the key-pool and key to use for the new object.</param>
        /// <param name="acl">Access control list for the new object.  May be null to use a default ACL</param>
        /// <param name="metadata">Metadata for the new object.  May be null for no metadata.</param>
        /// <param name="data">The initial contents of the object.  May be appended to later.  May be null to create an object with no content or a directory.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        /// <returns>the ObjectId of the newly-created object for references by ID.</returns>
        ObjectId CreateObjectFromSegmentWithKey(ObjectKey key, Acl acl, MetadataList metadata,
                ArraySegment<byte> data, String mimeType);

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
        ObjectId CreateObjectFromSegmentWithKey(ObjectKey key, Acl acl, MetadataList metadata,
                ArraySegment<byte> data, String mimeType, Checksum checksum);

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
        ObjectId CreateObjectFromStreamWithKey(ObjectKey key, Acl acl, MetadataList metadata,
                Stream data, long streamLength, String mimeType, Checksum checksum);

        /// <summary>
        /// Updates an object in the cloud.
        /// </summary>
        /// <param name="id">The ID of the object to update</param>
        /// <param name="acl">Access control list for the new object. Optional, set to NULL to leave the ACL unchanged.</param>
        /// <param name="metadata">Metadata list for the new object.  Optional, set to NULL for no changes to the metadata.</param>
        /// <param name="extent">portion of the object to update.  May be null to indicate the whole object is to be replaced.  If not null, the extent size must match the data size.</param>
        /// <param name="data">The new contents of the object.  May be appended to later. Optional, set to null for no content changes.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        void UpdateObject(Identifier id, Acl acl, MetadataList metadata,
                Extent extent, byte[] data, string mimeType);

        /// <summary>
        /// Updates an object in the cloud.
        /// </summary>
        /// <param name="id">The ID of the object to update</param>
        /// <param name="acl">Access control list for the new object. Optional, set to NULL to leave the ACL unchanged.</param>
        /// <param name="metadata">Metadata list for the new object.  Optional, set to NULL for no changes to the metadata.</param>
        /// <param name="extent">portion of the object to update.  May be null to indicate the whole object is to be replaced.  If not null, the extent size must match the data size.</param>
        /// <param name="data">The new contents of the object.  May be appended to later. Optional, set to null for no content changes.</param>
        /// <param name="checksum">the checksum object to use to compute checksums.  If you're doing incremental updates after the create, include the same object in subsequent calls.  Can be null to omit checksums.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        void UpdateObject(Identifier id, Acl acl, MetadataList metadata,
                Extent extent, byte[] data, string mimeType, Checksum checksum);


        /// <summary>
        /// Updates an object in the cloud.
        /// </summary>
        /// <param name="id">The ID of the object to update</param>
        /// <param name="acl">Access control list for the new object. Optional, set to NULL to leave the ACL unchanged.</param>
        /// <param name="metadata">Metadata list for the new object.  Optional, set to NULL for no changes to the metadata.</param>
        /// <param name="extent">portion of the object to update.  May be null to indicate the whole object is to be replaced.  If not null, the extent size must match the data size.</param>
        /// <param name="data">The new contents of the object.  May be appended to later. Optional, set to null for no content changes.</param>
        /// <param name="mimeType">the MIME type of the content.  Optional, may be null.  If data is non-null and mimeType is null, the MIME type will default to application/octet-stream.</param>
        void UpdateObjectFromSegment(Identifier id, Acl acl, MetadataList metadata,
                Extent extent, ArraySegment<byte> data, string mimeType);

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
        void UpdateObjectFromSegment(Identifier id, Acl acl, MetadataList metadata,
                Extent extent, ArraySegment<byte> data, string mimeType, Checksum checksum);

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
        void UpdateObjectFromStream(Identifier id, Acl acl, MetadataList metadata, Extent extent, Stream data, long streamLength, string mimeType, Checksum checksum);

        /// <summary>
        /// Fetches the user metadata for the object.
        /// </summary>
        /// <param name="id">the identifier of the object whose user metadata to fetch.</param>
        /// <param name="tags">A list of user metadata tags to fetch.  Optional.  If null, all user metadata will be fetched.</param>
        /// <returns>The list of user metadata for the object.</returns>
        MetadataList GetUserMetadata(Identifier id, MetadataTags tags);

        /// <summary>
        /// Fetches the system metadata for the object.
        /// </summary>
        /// <param name="id">the identifier of the object whose system metadata to fetch.</param>
        /// <param name="tags">A list of system metadata tags to fetch.  Optional.  If null, all metadata will be fetched.</param>
        /// <returns>The list of system metadata for the object.</returns>
        MetadataList GetSystemMetadata(Identifier id, MetadataTags tags);

        /// <summary>
        /// Reads an object's content.
        /// </summary>
        /// <param name="id">the identifier of the object whose content to read.</param>
        /// <param name="extent">the portion of the object data to read.  Optional.  If null, the entire object will be read.</param>
        /// <param name="buffer">the buffer to use to read the extent.  Must be large enough to read the response or an error will be thrown.  If null, a buffer will be allocated to hold the response data.  If you pass a buffer that is larger than the extent, only extent.getSize() bytes will be valid.</param>
        /// <returns>A byte array containing the requested content.</returns>
        byte[] ReadObject(Identifier id, Extent extent, byte[] buffer);

        /// <summary>
        /// Fetches an object's content as a stream
        /// </summary>
        /// <param name="id">the identifier of the object whose content to read.</param>
        /// <param name="extent">the portion of the object data to read.  Optional.  If null, the entire object will be read.</param>
        /// <returns></returns>
        ReadObjectStreamResponse ReadObjectStream(Identifier id, Extent extent);

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
        byte[] ReadObject(Identifier id, Extent extent, byte[] buffer, Checksum checksum);

        /// <summary>
        /// Reads content from multiple extents within an object using a single call.
        /// </summary>
        /// <param name="id">the identifier of the object whose content to read.</param>
        /// <param name="extents">the extents of the object data to read.</param>
        /// <returns>A MultipartEntity, which is also a List&lt;MultipartPart&gt;</MultipartPart>,
        /// but provides a method to aggregate data from the parts into a single byte array.</returns>
        MultipartEntity ReadObjectExtents(Identifier id, params Extent[] extents);

        /// <summary>
        /// Deletes an object from the cloud.
        /// </summary>
        /// <param name="id">The identifier of the object to delete.</param>
        void DeleteObject(Identifier id);

        /// <summary>
        /// Returns an object's ACL
        /// </summary>
        /// <param name="id">The identifier of the object whose ACL to read</param>
        /// <returns>The object's ACL</returns>
        Acl GetAcl(Identifier id);

        /// <summary>
        /// Sets the access control list on the object.
        /// </summary>
        /// <param name="id">The identifier of the object whose ACL to change</param>
        /// <param name="acl">The new ACL for the object.</param>
        void SetAcl(Identifier id, Acl acl);

        /// <summary>
        /// Writes the metadata into the object. If the tag does not exist, it is created and set to the corresponding value. If the tag exists, the existing value is replaced. 
        /// </summary>
        /// <param name="id">The identifier of the object to update</param>
        /// <param name="metadata">Metadata to write to the object.</param>
        void SetUserMetadata(Identifier id, MetadataList metadata);

        /// <summary>
        /// Deletes metadata items from an object.
        /// </summary>
        /// <param name="id">The identifier of the object whose metadata to delete.</param>
        /// <param name="tags">The list of metadata tags to delete.</param>
        void DeleteUserMetadata(Identifier id, MetadataTags tags);

        /// <summary>
        /// Lists the versions of an object.
        /// </summary>
        /// <param name="id">The object whose versions to list.</param>
        /// <returns>The list of versions of the object.  If the object does not have any versions, the array will be empty.</returns>
        List<ObjectId> ListVersions(Identifier id);

        /// <summary>
        /// Creates a new immutable version of an object.
        /// </summary>
        /// <param name="id">The object to version</param>
        /// <returns>The id of the newly created version</returns>
        ObjectId VersionObject(Identifier id);

        /// <summary>
        /// Lists all objects with the given tag.
        /// </summary>
        /// <param name="tag">The tag to search for</param>
        /// <returns>The list of objects with the given tag.  If no objects are found the array will be empty.</returns>
        /// <exception cref="T:EsuApiLib.EsuException">if no objects are found (code 1003)</exception>
        [Obsolete("Use the version of ListObjects with the ListOptions parameter to ensure you get all results.")]
        List<ObjectId> ListObjects(MetadataTag tag);

        /// <summary>
        /// Lists all objects with the given tag.
        /// </summary>
        /// <param name="tag">The tag to search for</param>
        /// <param name="options">Options for listing the objects. After calling ListObjects, be sure to check the value of the token property to see if there are additional results.</param>
        /// <returns>The list of objects with the given tag.  If no objects are found the array will be empty.</returns>
        /// <exception cref="T:EsuApiLib.EsuException">if no objects are found (code 1003)</exception>
        List<ObjectResult> ListObjects(MetadataTag tag, ListOptions options);

        /// <summary>
        /// Lists all objects with the given tag.
        /// </summary>
        /// <param name="tag">The tag to search for</param>
        /// <returns>The list of objects with the given tag.  If no objects are found the array will be empty.</returns>
        /// <exception cref="T:EsuApiLib.EsuException">if no objects are found (code 1003)</exception>
        [Obsolete("Use the version of ListObjects with the ListOptions parameter to ensure you get all results.")]
        List<ObjectId> ListObjects(string tag);

        /// <summary>
        /// Lists all objects with the given tag.
        /// </summary>
        /// <param name="tag">The tag to search for</param>
        /// <param name="options">Options for listing the objects. After calling ListObjects, be sure to check the value of the token property to see if there are additional results.</param>
        /// <returns>The list of objects with the given tag.  If no objects are found the array will be empty.</returns>
        /// <exception cref="T:EsuApiLib.EsuException">if no objects are found (code 1003)</exception>
        List<ObjectResult> ListObjects(string tag, ListOptions options);

        /// <summary>
        /// Lists all objects with the given tag.  This method returns both the objects' IDs as well
        /// as their metadata.
        /// </summary>
        /// <param name="tag">Tag the tag to search for</param>
        /// <returns>The list of objects with the given tag.  If no objects are found the array will be empty.</returns>
        /// <exception cref="T:EsuApiLib.EsuException">if no objects are found (code 1003)</exception>
        [Obsolete("Use the version of ListObjects with the ListOptions parameter for greater flexibility")]
        List<ObjectResult> ListObjectsWithMetadata(MetadataTag tag);

        /// <summary>
        /// Lists all objects with the given tag.  This method returns both the objects' IDs as well
        /// as their metadata.
        /// </summary>
        /// <param name="tag">Tag the tag to search for</param>
        /// <returns>The list of objects with the given tag.  If no objects are found the array will be empty.</returns>
        /// <exception cref="T:EsuApiLib.EsuException">if no objects are found (code 1003)</exception>
        [Obsolete("Use the version of ListObjects with the ListOptions parameter for greater flexibility")]
        List<ObjectResult> ListObjectsWithMetadata(string tag);

        /// <summary>
        /// Returns the set of listable tags for the current Tennant.
        /// </summary>
        /// <param name="tag">The tag whose children to list.  If null, only toplevel tags will be returned.</param>
        /// <returns>The list of listable tags.</returns>
        MetadataTags GetListableTags(MetadataTag tag);

        /// <summary>
        /// Returns the set of listable tags for the current Tennant.
        /// </summary>
        /// <param name="tag">The tag whose children to list.  If null, only toplevel tags will be returned.</param>
        /// <returns>The list of listable tags.</returns>
        MetadataTags GetListableTags(string tag);


        /// <summary>
        /// Returns the list of user metadata tags assigned to the object.
        /// </summary>
        /// <param name="id">The object whose metadata tags to list</param>
        /// <returns>The list of user metadata tags assigned to the object</returns>
        MetadataTags ListUserMetadataTags(Identifier id);

        ///// <summary>
        ///// Executes a query for objects matching the specified XQuery string.
        ///// </summary>
        ///// <param name="xquery">The XQuery string to execute against the cloud.</param>
        ///// <returns>The list of objects matching the query.  If no objects are found, the array will be empty.</returns>
        //List<ObjectId> QueryObjects( string xquery );

        /// <summary>
        /// Lists the contents of a directory.
        /// </summary>
        /// <param name="path">the path to list.  Must be a directory.</param>
        /// <returns>the directory entries in the directory.</returns>
        [Obsolete("Use the version of ListDirectory with ListOptions for greater flexibility and to ensure you get all results in a large result set")]
        List<DirectoryEntry> ListDirectory(ObjectPath path);

        /// <summary>
        /// Lists the contents of a directory.
        /// </summary>
        /// <param name="path">the path to list.  Must be a directory.</param>
        /// <param name="options">Options for listing the objects. After calling 
        /// ListObjects, be sure to check the value of the token property to see 
        /// if there are additional results.</param>
        /// <returns>the directory entries in the directory.</returns>
        List<DirectoryEntry> ListDirectory(ObjectPath path, ListOptions options);

        /// <summary>
        /// Returns all of an object's metadata and its ACL in
        /// one call.
        /// </summary>
        /// <param name="id">the object's identifier.</param>
        /// <returns>the object's metadata</returns>
        ObjectMetadata GetAllMetadata(Identifier id);

        /// <summary>
        /// An Atmos user (UID) can construct a pre-authenticated URL to an 
        /// object, which may then be used by anyone to retrieve the 
        /// object (e.g., through a browser). This allows an Atmos user 
        /// to let a non-Atmos user download a specific object. The 
        /// entire object/file is read.
        /// </summary>
        /// <param name="id">the object to generate the URL for</param>
        /// <param name="expiration">expiration the expiration date of the URL.  Note, be sure your expiration is in UTC (DateTimeKind.Utc)</param>
        /// <returns>a URL that can be used to share the object's content</returns>
        [Obsolete("This method has the wrong capitalization and will be removed in a future version.  Use GetShareableUrl instead")]
        Uri getShareableUrl(Identifier id, DateTime expiration);

        /// <summary>
        /// An Atmos user (UID) can construct a pre-authenticated URL to an 
        /// object, which may then be used by anyone to retrieve the 
        /// object (e.g., through a browser). This allows an Atmos user 
        /// to let a non-Atmos user download a specific object. The 
        /// entire object/file is read.
        /// </summary>
        /// <param name="id">the object to generate the URL for</param>
        /// <param name="expiration">expiration the expiration date of the URL.  Note, be sure your expiration is in UTC (DateTimeKind.Utc)</param>
        /// <returns>a URL that can be used to share the object's content</returns>
        Uri GetShareableUrl(Identifier id, DateTime expiration);

        /// <summary>
        /// Creates a shareable URL with the specified content-disposition.  This disposition value will be returned in the Content-Disposition response header.
        /// </summary>
        /// <param name="id">the object to generate the URL for</param>
        /// <param name="expiration">expiration the expiration date of the URL.  Note, be sure to ensure your expiration is in UTC (DateTimeKind.Utc)</param>
        /// <param name="disposition">the value that will be sent by the server in the Content-Disposition response header</param>
        /// <returns>a URL that can be used to share the object's content</returns>
        Uri GetShareableUrl(Identifier id, DateTime expiration, string disposition);

        /// <summary>
        /// Gets the UID used for this object's connections.
        /// </summary>
        /// <returns>The connection's UID</returns>
        string GetUid();

        /// <summary>
        /// Gets the name or IP of the host this object connects to.
        /// </summary>
        /// <returns>The hostname or IP as a string</returns>
        string GetHost();

        /// <summary>
        /// Gets the port number this object connects to.
        /// </summary>
        /// <returns>The port number</returns>
        int GetPort();

        /// <summary>
        /// Deletes a version from an object.  You cannot specify the base version
        /// of an object.
        /// </summary>
        /// <param name="vId">The ObjectID of the version to delete.</param>
        void DeleteVersion(ObjectId vId);

        /// <summary>
        /// Restores a version of an object to the base version (i.e. "promote" an 
        /// old version to the current version).
        /// </summary>
        /// <param name="id">Base object ID (target of the restore)</param>
        /// <param name="vId">Version object ID to restore</param>
        void RestoreVersion(ObjectId id, ObjectId vId);

        /// <summary>
        /// Renames a file or directory within the namespace.
        /// </summary>
        /// <param name="source">The file or directory to rename</param>
        /// <param name="destination">The new path for the file or directory</param>
        /// <param name="force">If true, the desination file or 
        /// directory will be overwritten.  Directories must be empty to be 
        /// overwritten.  Also note that overwrite operations on files are
        /// not synchronous; a delay may be required before the object is
        /// available at its destination.</param>
        void Rename(ObjectPath source, ObjectPath destination, bool force);

        /// <summary>
        /// Gets information about the connected service.  Currently, this is
        /// only the version of Atmos.
        /// </summary>
        /// <returns></returns>
        ServiceInformation GetServiceInformation();

        /// <summary>
        /// Gets Replica, Expiration, and Retention information for an object
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        ObjectInfo GetObjectInfo(Identifier id);

        /// <summary>
        /// Creates an anonymous access token using the specified policy and ACL
        /// </summary>
        /// <param name="id">identifier of the target object for the access token.</param>
        /// <param name="policy">the token policy for the new access token.</param>
        /// <param name="acl">the ACL that will be assigned to objects created using this access token.</param>
        /// <returns>The URL of the access token.</returns>
        Uri CreateAccessToken( Identifier id, PolicyType policy, Acl acl );

        /// <summary>
        /// Retrieves details about the specified access token. Implementation simply extracts the token ID from the URL and calls GetAccessToken(String).
        /// </summary>
        /// <param name="tokenUri">The URL of the access token.</param>
        /// <returns></returns>
        AccessTokenType GetAccessToken( Uri tokenUri );

        /// <summary>
        /// Retrieves details about the specified access token.
        /// </summary>
        /// <param name="accessTokenId">The ID of the access token.</param>
        /// <returns></returns>
        AccessTokenType GetAccessToken( String accessTokenId );

        /// <summary>
        /// Deletes the specified access token. Implementation simply extracts the token ID from the URL and calls deleteAccessToken(String).
        /// </summary>
        /// <param name="tokenUri">The URL of the access token.</param>
        void DeleteAccessToken(Uri tokenUri);

        /// <summary>
        /// Deletes the specified access token.
        /// </summary>
        /// <param name="accessTokenId">The ID of the access token.</param>
        void DeleteAccessToken( String accessTokenId );

        /// <summary>
        /// Lists all access tokens owned by the user using the options provided.
        /// </summary>
        /// <param name="options">Options for listing the objects. After calling 
        /// ListAccessTokens, be sure to check the value of the token property to see 
        /// if there are additional results.</param>
        /// <returns>The list of access tokens that the user has created</returns>
        List<AccessTokenType> ListAccessTokens( ListOptions options );
    }
}
