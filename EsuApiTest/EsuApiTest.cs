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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using EsuApiLib.Multipart;

namespace EsuApiLib {
    /// <summary>
    /// Implements testcase functionality that is common to all implementations
    /// of the EsuApi interface.
    /// </summary>
    [TestClass()]
    public abstract class EsuApiTest {
        private static readonly string TESTDIR = "/test_" + typeof(EsuApiTest).Name + "/";


        /// <summary>
        /// The EsuApi object used in the tests.
        /// </summary>
        protected EsuApi esu;
        private List<Identifier> cleanup;
        private Random rand = new Random();

        
        /// <summary>
        /// Tear down after a test is run.  Cleans up objects that were created
        /// during the test.  Set cleanUp=false to disable this behavior.
        /// </summary>
        [TestCleanup()]
        public void tearDown() {
            foreach( Identifier cleanItem in cleanup ) {
                try {
                    this.esu.DeleteObject( cleanItem );
                } catch( Exception e ) {
                    Console.WriteLine( "Failed to delete " + cleanItem + ": " + e.Message );
                }
            }
            try
            { // if the test directory exists, recursively delete it
                this.esu.GetSystemMetadata(new ObjectPath(TESTDIR), null);
                deleteRecursively(new ObjectPath(TESTDIR));
            }
            catch (EsuException e)
            {
                if (e.Code != 1003)
                {
                    Debug.WriteLine("Could not delete test dir: " + e.Message);
                }
            }
        }

        protected void deleteRecursively( ObjectPath path ) {
            if ( path.IsDirectory() ) {
                ListOptions options = new ListOptions();
                do {
                    foreach ( DirectoryEntry entry in this.esu.ListDirectory( path, options ) ) {
                        deleteRecursively( entry.Path );
                    }
                } while ( options.Token != null );
            }
            this.esu.DeleteObject( path );
        }

        /// <summary>
        /// Sets up environment before testcases are run
        /// </summary>
        [TestInitialize()]
        public virtual void SetUp() {
            cleanup = new List<Identifier>();
        }

        //
        // TESTS START HERE
        //

        /// <summary>
        /// Test creating one empty object.  No metadata, no content.
        /// </summary>
        [TestMethod()]
        public void testCreateEmptyObject() {
            ObjectId id = this.esu.CreateObject( null, null, null, null );
            Assert.IsNotNull( id, "null ID returned" );
            cleanup.Add( id );

            // Read back the content
            string content = Encoding.UTF8.GetString( this.esu.ReadObject( id, null, null ) );
            Assert.AreEqual( "", content, "object content wrong" );

        }

        /// <summary>
        /// Test creating an object with content but without metadata
        /// </summary>
        [TestMethod()]
        public void testCreateObjectWithContent() {
            ObjectId id = this.esu.CreateObject( null, null, Encoding.UTF8.GetBytes( "hello" ), "text/plain" );
            Assert.IsNotNull( id, "null ID returned" );
            cleanup.Add( id );

            // Read back the content
            string content = Encoding.UTF8.GetString( this.esu.ReadObject( id, null, null ) );
            Assert.AreEqual( "hello", content, "object content wrong" );
        }

        [TestMethod()]
        public void testCreateObjectFromStream()
        {
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes("hello"));

            ObjectId id = this.esu.CreateObjectFromStream(null, null, ms, 5, "text/plain", null);
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            // Read back the content
            string content = Encoding.UTF8.GetString(this.esu.ReadObject(id, null, null));
            Assert.AreEqual("hello", content, "object content wrong");
        }

        [TestMethod()]
        public void testCreateObjectFromStreamOnPath()
        {
            string dir = rand8char();
            string file = rand8char();
            ObjectPath op = new ObjectPath("/" + dir + "/" + file);
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes("hello"));

            ObjectId id = this.esu.CreateObjectFromStreamOnPath(op, null, null, ms, 5, "text/plain", null);
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            // Read back the content
            string content = Encoding.UTF8.GetString(this.esu.ReadObject(id, null, null));
            Assert.AreEqual("hello", content, "object content wrong");
        }

        [TestMethod()]
        public void testCreateObjectUnicodePath()
        {
            string dir = rand8char();
            ObjectPath op = new ObjectPath("/" + dir + "/спасибо.txt");
            MemoryStream ms = new MemoryStream();

            ObjectId id = this.esu.CreateObjectOnPath(op, null, null, Encoding.UTF8.GetBytes("спасибо"), "text/plain", null);
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            // Read back the content
            string content = Encoding.UTF8.GetString(this.esu.ReadObject(id, null, null));
            Assert.AreEqual("спасибо", content, "object content wrong");
        }

        [TestMethod()]
        public void testUpdateObjectFromStream()
        {
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes("hello"));

            ObjectId id = this.esu.CreateObjectFromStream(null, null, ms, 5, "text/plain", null);
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            // Read back the content
            string content = Encoding.UTF8.GetString(this.esu.ReadObject(id, null, null));
            Assert.AreEqual("hello", content, "object content wrong");

            ms = new MemoryStream(Encoding.UTF8.GetBytes("And now for something different"));
            this.esu.UpdateObjectFromStream(id, null, null, null, ms, ms.Length, "text/plain", null);
            content = Encoding.UTF8.GetString(this.esu.ReadObject(id, null, null));
            Assert.AreEqual("And now for something different", content, "object content wrong");

            // Update an extent
            Extent extent = new Extent(4, 3);
            ms = new MemoryStream(Encoding.UTF8.GetBytes("how"));
            this.esu.UpdateObjectFromStream(id, null, null, extent, ms, ms.Length, "text/plain", null);
            content = Encoding.UTF8.GetString(this.esu.ReadObject(id, null, null));
            Assert.AreEqual("And how for something different", content, "object content wrong after update");
        }


        /// <summary>
        /// Test creating an object with metadata but no content.
        /// </summary>
        [TestMethod()]
        public void testCreateObjectWithMetadata() {
            MetadataList mlist = new MetadataList();
            Metadata listable = new Metadata( "listable", "foo", true );
            Metadata unlistable = new Metadata( "unlistable", "bar", false );
            Metadata listable2 = new Metadata( "listable2", "foo2 foo2", true );
            Metadata unlistable2 = new Metadata( "unlistable2", "bar2 bar2", false );
            Metadata empty = new Metadata("empty", "", false);
            //Metadata withEqual = new Metadata("withEqual", "x=y=z", false);
            mlist.AddMetadata(listable);
            mlist.AddMetadata( unlistable );
            mlist.AddMetadata( listable2 );
            mlist.AddMetadata( unlistable2 );
            mlist.AddMetadata( empty );
            //mlist.AddMetadata( withEqual );
            ObjectId id = this.esu.CreateObject( null, mlist, null, null );
            Assert.IsNotNull( id, "null ID returned" );
            cleanup.Add( id );

            // Read and validate the metadata
            MetadataList meta = this.esu.GetUserMetadata( id, null );
            Assert.AreEqual( "foo", meta.GetMetadata( "listable" ).Value, "value of 'listable' wrong" );
            Assert.AreEqual( "foo2 foo2", meta.GetMetadata( "listable2" ).Value, "value of 'listable2' wrong" );
            Assert.AreEqual( "bar", meta.GetMetadata( "unlistable" ).Value, "value of 'unlistable' wrong" );
            Assert.AreEqual( "bar2 bar2", meta.GetMetadata( "unlistable2" ).Value, "value of 'unlistable2' wrong" );
            Assert.AreEqual("", meta.GetMetadata("empty").Value, "value of 'empty' wrong");
            //Assert.AreEqual("x=y=z", meta.GetMetadata("withEqual").Value, "value of 'withEqual' wrong");

        }

        /// <summary>
        /// Test creating an object with metadata including a nonbreaking space
        /// </summary>
        [TestMethod()]
        public void testCreateObjectWithNBSP()
        {
            MetadataList mlist = new MetadataList();
            Metadata nbspValue = new Metadata("nbspvalue", "Nobreak\u00A0Value", false);
            Metadata nbspName = new Metadata("Nobreak\u00A0Name", "regular text here", false);
            Console.WriteLine("NBSP Value: " + nbspValue);
            Console.WriteLine("NBSP Name: " + nbspName);

            mlist.AddMetadata(nbspValue);
            mlist.AddMetadata(nbspName);
            ObjectId id = this.esu.CreateObject(null, mlist, null, null);
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            // Read and validate the metadata
            MetadataList meta = this.esu.GetUserMetadata(id, null);
            Console.WriteLine("Read Back:");
            Console.WriteLine("NBSP Value: " + meta.GetMetadata("nbspvalue"));
            Console.WriteLine("NBSP Name: " + meta.GetMetadata("Nobreak\u00A0Name"));
            Assert.AreEqual("Nobreak\u00A0Value", meta.GetMetadata("nbspvalue").Value, "value of 'nobreakvalue' wrong");

        }


        /// <summary>
        /// Test creating an object with metadata but no content.
        /// </summary>
        [TestMethod()]
        public void testCreateObjectWithMetadataNormalizeSpaces()
        {
            MetadataList mlist = new MetadataList();
            Metadata listable = new Metadata("listable", "foo", true);
            Metadata unlistable = new Metadata("unlistable", "bar  bar", false);
            Metadata listable2 = new Metadata("listable2", "foo2    foo2", true);
            Metadata unlistable2 = new Metadata("unlistable2", "bar2       bar2", false);
            Metadata empty = new Metadata("empty", "", false);
            //Metadata withEqual = new Metadata("withEqual", "x=y=z", false);
            mlist.AddMetadata(listable);
            mlist.AddMetadata(unlistable);
            mlist.AddMetadata(listable2);
            mlist.AddMetadata(unlistable2);
            mlist.AddMetadata(empty);
            //mlist.AddMetadata(withEqual);
            ObjectId id = this.esu.CreateObject(null, mlist, null, null);
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            // Read and validate the metadata
            MetadataList meta = this.esu.GetUserMetadata(id, null);
            Assert.AreEqual("foo", meta.GetMetadata("listable").Value, "value of 'listable' wrong");
            Assert.AreEqual("foo2    foo2", meta.GetMetadata("listable2").Value, "value of 'listable2' wrong");
            Assert.AreEqual("bar  bar", meta.GetMetadata("unlistable").Value, "value of 'unlistable' wrong");
            Assert.AreEqual("bar2       bar2", meta.GetMetadata("unlistable2").Value, "value of 'unlistable2' wrong");
            Assert.AreEqual("", meta.GetMetadata("empty").Value, "value of 'empty' wrong");
            //Assert.AreEqual("x=y=z", meta.GetMetadata("withEqual").Value, "value of 'withEqual' wrong");

        }



        /// <summary>
        /// Test reading an object's content
        /// </summary>
        [TestMethod()]
        public void testReadObject() {
            ObjectId id = this.esu.CreateObject( null, null, Encoding.UTF8.GetBytes( "hello" ), "text/plain" );
            Assert.IsNotNull( id, "null ID returned" );
            cleanup.Add( id );

            // Read back the content
            string content = Encoding.UTF8.GetString( this.esu.ReadObject( id, null, null ) );
            Assert.AreEqual( "hello", content, "object content wrong" );

            // Read back only 2 bytes
            Extent extent = new Extent( 1, 2 );
            content = Encoding.UTF8.GetString( this.esu.ReadObject( id, extent, null ) );
            Assert.AreEqual( "el", content, "partial object content wrong" );               
        }

        [TestMethod()]
        public void testReadObjectStream()
        {
            Acl acl = new Acl();
            acl.AddGrant(new Grant(new Grantee(getUid(esu.GetUid()), Grantee.GRANTEE_TYPE.USER), Permission.FULL_CONTROL));
            acl.AddGrant(new Grant(Grantee.OTHER, Permission.READ));
            MetadataList mlist = new MetadataList();
            Metadata listable = new Metadata("listable", "foo", true);
            Metadata unlistable = new Metadata("unlistable", "bar", false);
            Metadata listable2 = new Metadata("listable2", "foo2 foo2", true);
            Metadata unlistable2 = new Metadata("unlistable2", "bar2 bar2", false);
            Metadata empty = new Metadata("empty", "", false);
            //Metadata withEqual = new Metadata("withEqual", "x=y=z", false);
            mlist.AddMetadata(listable);
            mlist.AddMetadata(unlistable);
            mlist.AddMetadata(listable2);
            mlist.AddMetadata(unlistable2);
            mlist.AddMetadata(empty);

            ObjectId id = esu.CreateObject(acl, mlist, Encoding.UTF8.GetBytes("hello"), "text/plain; charset=UTF-8");
            cleanup.Add(id);

            // Read back
            ReadObjectStreamResponse response = esu.ReadObjectStream(id, null);

            // Check content
            Assert.AreEqual(5, response.Length, "Content length incorrect");
            Assert.AreEqual("text/plain; charset=UTF-8", response.ContentType, "Content type incorrect");
            byte[] buffer = new byte[1024];
            int count = response.Content.Read(buffer, 0, buffer.Length);
            response.Close();

            Assert.AreEqual(5, count, "Incorrect number of bytes read from stream");
            string content = Encoding.UTF8.GetString(buffer, 0, count);
            Assert.AreEqual("hello", content, "Stream content incorrect");

            // Check metadata
            MetadataList meta = response.Metadata;
            Assert.AreEqual("foo", meta.GetMetadata("listable").Value, "value of 'listable' wrong");
            Assert.AreEqual("foo2 foo2", meta.GetMetadata("listable2").Value, "value of 'listable2' wrong");
            Assert.AreEqual("bar", meta.GetMetadata("unlistable").Value, "value of 'unlistable' wrong");
            Assert.AreEqual("bar2 bar2", meta.GetMetadata("unlistable2").Value, "value of 'unlistable2' wrong");
            Assert.AreEqual("", meta.GetMetadata("empty").Value, "value of 'empty' wrong");

            // Check ACL
            Acl newacl = response.Acl;
            Debug.WriteLine("Comparing " + newacl + " with " + acl);

            Assert.AreEqual(acl, newacl, "ACLs don't match");

            // Read a segment of the data back
            Extent extent = new Extent(1, 2);
            response = esu.ReadObjectStream(id, extent);
            count = response.Content.Read(buffer, 0, buffer.Length);
            response.Close();
            Assert.AreEqual(2, count, "Incorrect number of bytes read from stream");
            content = Encoding.UTF8.GetString(buffer, 0, count);
            Assert.AreEqual("el", content, "Stream content incorrect");

        }

        private string getUid(string uidstring)
        {
            // For ACLs, we just want the UID portion of the string, not the subtenantid.
            if (uidstring.Contains("/"))
            {
                return uidstring.Substring(uidstring.IndexOf("/") + 1);
            }
            else
            {
                return uidstring;
            }
        }

        /// <summary>
        /// Test reading an ACL back
        /// </summary>
        [TestMethod()]
        public void testReadAcl() {
            // Create an object with an ACL
            Acl acl = new Acl();
            acl.AddGrant( new Grant( new Grantee( getUid(esu.GetUid()), Grantee.GRANTEE_TYPE.USER ), Permission.FULL_CONTROL ) );
            acl.AddGrant( new Grant( Grantee.OTHER, Permission.READ ) );
            ObjectId id = this.esu.CreateObject( acl, null, null, null );
            Assert.IsNotNull( id, "null ID returned" );
            cleanup.Add( id );

            // Read back the ACL and make sure it matches
            Acl newacl = this.esu.GetAcl( id );
            Debug.WriteLine( "Comparing " + newacl + " with " + acl );

            Assert.AreEqual( acl, newacl, "ACLs don't match" );

        }

        /// <summary>
        /// Test reading back user metadata
        /// </summary>
        [TestMethod()]
        public void testGetUserMetadata() {
            // Create an object with user metadata
            MetadataList mlist = new MetadataList();
            Metadata listable = new Metadata( "listable", "foo", true );
            Metadata unlistable = new Metadata( "unlistable", "bar", false );
            Metadata listable2 = new Metadata( "listable2", "foo2 foo2", true );
            Metadata unlistable2 = new Metadata( "unlistable2", "bar2 bar2", false );
            mlist.AddMetadata( listable );
            mlist.AddMetadata( unlistable );
            mlist.AddMetadata( listable2 );
            mlist.AddMetadata( unlistable2 );
            ObjectId id = this.esu.CreateObject( null, mlist, null, null );
            Assert.IsNotNull( id, "null ID returned" );
            cleanup.Add( id );

            // Read only part of the metadata
            MetadataTags mtags = new MetadataTags();
            mtags.AddTag( new MetadataTag( "listable", true ) );
            mtags.AddTag( new MetadataTag( "unlistable", false ) );
            MetadataList meta = this.esu.GetUserMetadata( id, mtags );
            Assert.AreEqual( "foo", meta.GetMetadata( "listable" ).Value, "value of 'listable' wrong" );
            Assert.IsNull( meta.GetMetadata( "listable2" ), "value of 'listable2' should not have been returned" );
            Assert.AreEqual( "bar", meta.GetMetadata( "unlistable" ).Value, "value of 'unlistable' wrong" );
            Assert.IsNull( meta.GetMetadata( "unlistable2" ), "value of 'unlistable2' should not have been returned" );

        }

        /// <summary>
        /// Test deleting user metadata
        /// </summary>
        [TestMethod()]
        public void testDeleteUserMetadata() {
            // Create an object with metadata
            MetadataList mlist = new MetadataList();
            Metadata listable = new Metadata( "listable", "foo", true );
            Metadata unlistable = new Metadata( "unlistable", "bar", false );
            Metadata listable2 = new Metadata( "listable2", "foo2 foo2", true );
            Metadata unlistable2 = new Metadata( "unlistable2", "bar2 bar2", false );
            mlist.AddMetadata( listable );
            mlist.AddMetadata( unlistable );
            mlist.AddMetadata( listable2 );
            mlist.AddMetadata( unlistable2 );
            ObjectId id = this.esu.CreateObject( null, mlist, null, null );
            Assert.IsNotNull( id,"null ID returned" );
            cleanup.Add( id );

            // Delete a couple of the metadata entries
            MetadataTags mtags = new MetadataTags();
            mtags.AddTag( new MetadataTag( "listable2", true ) );
            mtags.AddTag( new MetadataTag( "unlistable2", false ) );
            this.esu.DeleteUserMetadata( id, mtags );

            // Read back the metadata for the object and ensure the deleted
            // entries don't exist
            MetadataList meta = this.esu.GetUserMetadata( id, null );
            Assert.AreEqual( "foo", meta.GetMetadata( "listable" ).Value, "value of 'listable' wrong" );
            Assert.IsNull( meta.GetMetadata( "listable2" ), "value of 'listable2' should not have been returned" );
            Assert.AreEqual( "bar", meta.GetMetadata( "unlistable" ).Value, "value of 'unlistable' wrong" );
            Assert.IsNull( meta.GetMetadata( "unlistable2" ), "value of 'unlistable2' should not have been returned" );
        }

        /// <summary>
        /// Test creating object versions
        /// </summary>
        [TestMethod()]
        public void testVersionObject()
        {
            // Create an object
            MetadataList mlist = new MetadataList();
            Metadata listable = new Metadata("listable", "foo", true);
            Metadata unlistable = new Metadata("unlistable", "bar", false);
            Metadata listable2 = new Metadata("listable2", "foo2 foo2", true);
            Metadata unlistable2 = new Metadata("unlistable2", "bar2 bar2", false);
            mlist.AddMetadata(listable);
            mlist.AddMetadata(unlistable);
            mlist.AddMetadata(listable2);
            mlist.AddMetadata(unlistable2);
            ObjectId id = this.esu.CreateObject(null, mlist, null, null);
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            // Version the object
            ObjectId vid = this.esu.VersionObject(id);
            cleanup.Add(vid);
            Assert.IsNotNull(vid, "null version ID returned");

            Assert.IsFalse(id.Equals(vid), "Version ID shoudn't be same as original ID");

            // Fetch the version and read its data
            MetadataList meta = this.esu.GetUserMetadata(vid, null);
            Assert.AreEqual("foo", meta.GetMetadata("listable").Value, "value of 'listable' wrong");
            Assert.AreEqual("foo2 foo2", meta.GetMetadata("listable2").Value, "value of 'listable2' wrong");
            Assert.AreEqual("bar", meta.GetMetadata("unlistable").Value, "value of 'unlistable' wrong");
            Assert.AreEqual("bar2 bar2", meta.GetMetadata("unlistable2").Value, "value of 'unlistable2' wrong");

        }

        /// <summary>
        /// Test listing the versions of an object
        /// </summary>
        [TestMethod()]
        public void testListVersions()
        {
            // Create an object
            MetadataList mlist = new MetadataList();
            Metadata listable = new Metadata("listable", "foo", true);
            Metadata unlistable = new Metadata("unlistable", "bar", false);
            Metadata listable2 = new Metadata("listable2", "foo2 foo2", true);
            Metadata unlistable2 = new Metadata("unlistable2", "bar2 bar2", false);
            mlist.AddMetadata(listable);
            mlist.AddMetadata(unlistable);
            mlist.AddMetadata(listable2);
            mlist.AddMetadata(unlistable2);
            ObjectId id = this.esu.CreateObject(null, mlist, null, null);
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            // Version the object
            ObjectId vid1 = this.esu.VersionObject(id);
            cleanup.Add(vid1);
            Assert.IsNotNull(vid1, "null version ID returned");
            ObjectId vid2 = this.esu.VersionObject(id);
            cleanup.Add(vid2);
            Assert.IsNotNull(vid2, "null version ID returned");

            // List the versions and ensure their IDs are correct
            List<ObjectId> versions = this.esu.ListVersions(id);
            Assert.AreEqual(2, versions.Count, "Wrong number of versions returned");
            Assert.IsTrue(versions.Contains(vid1), "version 1 not found in version list");
            Assert.IsTrue(versions.Contains(vid2), "version 2 not found in version list");
            //Assert.IsTrue(versions.Contains(id), "base object not found in version list");
        }

        /// <summary>
        /// Test listing the system metadata on an object
        /// </summary>
        [TestMethod()]
        public void testGetSystemMetadata() {
            // Create an object
            MetadataList mlist = new MetadataList();
            Metadata listable = new Metadata( "listable", "foo", true );
            Metadata unlistable = new Metadata( "unlistable", "bar", false );
            Metadata listable2 = new Metadata( "listable2", "foo2 foo2", true );
            Metadata unlistable2 = new Metadata( "unlistable2", "bar2 bar2", false );
            mlist.AddMetadata( listable );
            mlist.AddMetadata( unlistable );
            mlist.AddMetadata( listable2 );
            mlist.AddMetadata( unlistable2 );
            ObjectId id = this.esu.CreateObject( null, mlist, null, null );
            Assert.IsNotNull( id,"null ID returned" );
            cleanup.Add( id );

            // Read only part of the metadata
            MetadataTags mtags = new MetadataTags();
            mtags.AddTag( new MetadataTag( "atime", false ) );
            mtags.AddTag( new MetadataTag( "ctime", false ) );
            MetadataList meta = this.esu.GetSystemMetadata( id, mtags );
            Assert.IsNotNull( meta.GetMetadata( "atime" ), "value of 'atime' missing" );
            Assert.IsNull( meta.GetMetadata( "mtime" ), "value of 'mtime' should not have been returned" );
            Assert.IsNotNull( meta.GetMetadata( "ctime" ), "value of 'ctime' missing" );
            Assert.IsNull( meta.GetMetadata( "gid" ), "value of 'gid' should not have been returned" );
            Assert.IsNull( meta.GetMetadata( "listable" ), "value of 'listable' should not have been returned" );
        }

        /// <summary>
        /// Test listing the versions of an object
        /// </summary>
        public void testDeleteVersion()
        {
            // Create an object
            MetadataList mlist = new MetadataList();
            Metadata listable = new Metadata("listable", "foo", true);
            Metadata unlistable = new Metadata("unlistable", "bar", false);
            Metadata listable2 = new Metadata("listable2", "foo2 foo2", true);
            Metadata unlistable2 = new Metadata("unlistable2", "bar2 bar2", false);
            mlist.AddMetadata(listable);
            mlist.AddMetadata(unlistable);
            mlist.AddMetadata(listable2);
            mlist.AddMetadata(unlistable2);
            ObjectId id = this.esu.CreateObject(null, mlist, null, null);
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            // Version the object
            ObjectId vid1 = this.esu.VersionObject(id);
            cleanup.Add(vid1);
            Assert.IsNotNull(vid1, "null version ID returned");
            ObjectId vid2 = this.esu.VersionObject(id);
            cleanup.Add(vid2);
            Assert.IsNotNull(vid2, "null version ID returned");

            // List the versions and ensure their IDs are correct
            List<ObjectId> versions = this.esu.ListVersions(id);
            Assert.AreEqual(3, versions.Count, "Wrong number of versions returned");
            Assert.IsTrue(versions.Contains(vid1), "version 1 not found in version list");
            Assert.IsTrue(versions.Contains(vid2), "version 2 not found in version list");
            Assert.IsTrue(versions.Contains(id), "base object not found in version list");

            // Delete a version
            this.esu.DeleteVersion(vid2);
            versions = this.esu.ListVersions(id);
            Assert.AreEqual(2, versions.Count, "Wrong number of versions returned");
            Assert.IsTrue(versions.Contains(vid1), "version 1 not found in version list");
            Assert.IsFalse(versions.Contains(vid2), "version 2 found in version list (should be deleted)");
            Assert.IsTrue(versions.Contains(id), "base object not found in version list");
        }


        /// <summary>
        /// Test replacing an object with an older version.
        /// </summary>
        [TestMethod()]
        public void testRestoreVersion()
        {
            ObjectId id = this.esu.CreateObject(null, null, Encoding.UTF8.GetBytes("Base Version Content"), "text/plain");
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            // Version the object
            ObjectId vId = this.esu.VersionObject(id);

            // Update the object content
            this.esu.UpdateObject(id, null, null, null, Encoding.UTF8.GetBytes("Child Version Content -- You should never see me"), "text/plain");

            // Restore the original version
            this.esu.RestoreVersion(id, vId);

            // Read back the content
            string content = Encoding.UTF8.GetString(this.esu.ReadObject(id, null, null));
            Assert.AreEqual("Base Version Content", content, "object content wrong");
        }



        /// <summary>
        /// Test listing objects by a tag
        /// </summary>
        [TestMethod()]
        public void testListObjects()
        {
            // Create an object
            MetadataList mlist = new MetadataList();
            Metadata listable = new Metadata("listable", "foo", true);
            Metadata unlistable = new Metadata("unlistable", "bar", false);
            Metadata listable2 = new Metadata("listable2", "foo2 foo2", true);
            Metadata unlistable2 = new Metadata("unlistable2", "bar2 bar2", false);
            mlist.AddMetadata(listable);
            mlist.AddMetadata(unlistable);
            mlist.AddMetadata(listable2);
            mlist.AddMetadata(unlistable2);
            ObjectId id = this.esu.CreateObject(null, mlist, null, null);
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            // List the objects.  Make sure the one we created is in the list
            List<ObjectResult> objects = this.esu.ListObjects("listable", null);
            Assert.IsTrue(objects.Count > 0, "No objects returned");
            Assert.IsTrue(containsId(objects, id), "object not found in list");

            // Check for unlisted
            try
            {
                this.esu.ListObjects("unlistable", null);
                Assert.Fail("Exception not thrown!");
            }
            catch (EsuException e)
            {
                // This should happen.
                Assert.AreEqual(1003, e.Code, "Expected 1003 for not found");
            }
        }

        /// <summary>
        /// Test listing objects by a tag
        /// </summary>
        [TestMethod()]
        public void testListObjectsPaged()
        {
            // Create an object
            MetadataList mlist = new MetadataList();
            Metadata listable = new Metadata("listable", "foo", true);
            Metadata unlistable = new Metadata("unlistable", "bar", false);
            Metadata listable2 = new Metadata("listable2", "foo2 foo2", true);
            Metadata unlistable2 = new Metadata("unlistable2", "bar2 bar2", false);
            mlist.AddMetadata(listable);
            mlist.AddMetadata(unlistable);
            mlist.AddMetadata(listable2);
            mlist.AddMetadata(unlistable2);
            ObjectId id = this.esu.CreateObject(null, mlist, null, null);
            ObjectId id2 = this.esu.CreateObject(null, mlist, null, null);
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);
            cleanup.Add(id2);

            // List the objects.  Make sure the one we created is in the list
            ListOptions options = new ListOptions();
            options.Limit = 1;
            List<ObjectResult> objects = this.esu.ListObjects("listable", options);
            while (options.Token != null)
            {
                Debug.WriteLine("Continuing results with token " + options.Token);
                objects.AddRange(this.esu.ListObjects("listable", options));
            }
            Assert.IsTrue(objects.Count > 0, "No objects returned");
            Assert.IsTrue(containsId(objects, id), "object not found in list");
            Assert.IsTrue(containsId(objects, id2), "object2 not found in list");
        }


        private bool containsId(List<ObjectResult> objects, ObjectId id)
        {
            foreach (ObjectResult res in objects)
            {
                if (res.Id.Equals(id))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Test listing objects by a tag
        /// </summary>
        [TestMethod()]
        public void testListObjectsWithMetadata()
        {
            // Create an object
            MetadataList mlist = new MetadataList();
            Metadata listable = new Metadata("listable", "foo", true);
            Metadata unlistable = new Metadata("unlistable", "bar", false);
            Metadata listable2 = new Metadata("listable2", "foo2 foo2", true);
            Metadata unlistable2 = new Metadata("unlistable2", "bar2 bar2", false);
            mlist.AddMetadata(listable);
            mlist.AddMetadata(unlistable);
            mlist.AddMetadata(listable2);
            mlist.AddMetadata(unlistable2);
            ObjectId id = this.esu.CreateObject(null, mlist, null, null);
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            // List the objects.  Make sure the one we created is in the list
            ListOptions options = new ListOptions();
            options.IncludeMetadata = true;
            List<ObjectResult> objects = this.esu.ListObjects("listable", options);
            Assert.IsTrue(objects.Count > 0, "No objects returned");

            bool found = false;
            foreach (ObjectResult or in objects)
            {
                if (or.Id.Equals(id))
                {
                    found = true;
                    //Test metadata value
                    Assert.AreEqual("foo", or.meta.GetMetadata("listable").Value, "Metadata value wrong");
                }
            }

            Assert.IsTrue(found, "object not found in list");

            
        }

        /// <summary>
        /// Test fetching listable tags
        /// </summary>
        [TestMethod()]
        public void testGetListableTags() {
            // Create an object
            MetadataList mlist = new MetadataList();
            Metadata listable = new Metadata( "listable", "foo", true );
            Metadata unlistable = new Metadata( "unlistable", "bar", false );
            Metadata listable2 = new Metadata( "list/able/2", "foo2 foo2", true );
            Metadata unlistable2 = new Metadata( "list/able/not", "bar2 bar2", false );
            mlist.AddMetadata( listable );
            mlist.AddMetadata( unlistable );
            mlist.AddMetadata( listable2 );
            mlist.AddMetadata( unlistable2 );
            ObjectId id = this.esu.CreateObject( null, mlist, null, null );
            Assert.IsNotNull( id,"null ID returned" );
            cleanup.Add( id );

            // List tags.  Ensure our object's tags are in the list.
            MetadataTags tags = this.esu.GetListableTags( (string)null );
            Assert.IsTrue( tags.Contains( "listable" ), "listable tag not returned" );
            Assert.IsTrue( tags.Contains( "list" ), "list/able/2 root tag not returned" );
            Assert.IsFalse( tags.Contains( "list/able/not" ), "list/able/not tag returned" );

            // List child tags
            tags = this.esu.GetListableTags( "list/able" );
            Assert.IsFalse( tags.Contains( "listable" ), "non-child returned" );
            Assert.IsTrue( tags.Contains( "2" ), "list/able/2 tag not returned" );
            Assert.IsFalse( tags.Contains( "not" ), "list/able/not tag returned" );

        }

        /// <summary>
        /// Test listing the user metadata tags on an object
        /// </summary>
        [TestMethod()]
        public void testListUserMetadataTags() {
            // Create an object
            MetadataList mlist = new MetadataList();
            Metadata listable = new Metadata( "listable", "foo", true );
            Metadata unlistable = new Metadata( "unlistable", "bar", false );
            Metadata listable2 = new Metadata( "list/able/2", "foo2 foo2", true );
            Metadata unlistable2 = new Metadata( "list/able/not", "bar2 bar2", false );
            mlist.AddMetadata( listable );
            mlist.AddMetadata( unlistable );
            mlist.AddMetadata( listable2 );
            mlist.AddMetadata( unlistable2 );
            ObjectId id = this.esu.CreateObject( null, mlist, null, null );
            Assert.IsNotNull( id,"null ID returned" );
            cleanup.Add( id );

            // List tags
            MetadataTags tags = this.esu.ListUserMetadataTags( id );
            Assert.IsTrue( tags.Contains( "listable" ), "listable tag not returned" );
            Assert.IsTrue( tags.Contains( "list/able/2" ), "list/able/2 tag not returned" );
            Assert.IsTrue( tags.Contains( "unlistable" ), "unlistable tag not returned" );
            Assert.IsTrue( tags.Contains( "list/able/not" ), "list/able/not tag not returned" );
            Assert.IsFalse( tags.Contains( "unknowntag" ), "unknown tag returned" );

            // Check listable flag
            Assert.IsTrue( tags.GetTag( "listable" ).Listable, "'listable' is not listable" );
            Assert.IsTrue( tags.GetTag( "list/able/2" ).Listable, "'list/able/2' is not listable" );
            Assert.IsFalse( tags.GetTag( "unlistable" ).Listable, "'unlistable' is listable" );
            Assert.IsFalse( tags.GetTag( "list/able/not" ).Listable, "'list/able/not' is listable" );
        }

        ///// <summary>
        ///// Test executing a query.
        ///// </summary>
        //[TestMethod()]
        //public void testQueryObjects() {
        //    // Create an object
        //    MetadataList mlist = new MetadataList();
        //    Metadata listable = new Metadata( "listable", "foo", true );
        //    Metadata unlistable = new Metadata( "unlistable", "bar", false );
        //    Metadata listable2 = new Metadata( "list/able/2", "foo2 foo2", true );
        //    Metadata unlistable2 = new Metadata( "list/able/not", "bar2 bar2", false );
        //    mlist.AddMetadata( listable );
        //    mlist.AddMetadata( unlistable );
        //    mlist.AddMetadata( listable2 );
        //    mlist.AddMetadata( unlistable2 );
        //    ObjectId id = this.esu.CreateObject( null, mlist, null, null );
        //    Assert.IsNotNull( id,"null ID returned" );
        //    cleanup.Add( id );

        //    // Query for all objects for the current UID
        //    string query = "for $h in collection() where $h/maui:MauiObject[uid=\"" +
        //        esu.GetUid() +"\"] return $h";
        //    Debug.WriteLine( "Query: " + query );
        //    List<ObjectId> objects = this.esu.QueryObjects( query );

        //    // Ensure the search results contains the object we just created
        //    Assert.IsTrue( objects.Contains( id ), "object not found in list" );

        //}

        /// <summary>
        /// Tests updating an object's metadata
        /// </summary>
        [TestMethod()]
        public void testUpdateObjectMetadata() {
            // Create an object
            MetadataList mlist = new MetadataList();
            Metadata unlistable = new Metadata( "unlistable", "foo", false );
            mlist.AddMetadata( unlistable );
            ObjectId id = this.esu.CreateObject( null, mlist, Encoding.UTF8.GetBytes( "hello" ), null );
            Assert.IsNotNull( id,"null ID returned" );
            cleanup.Add( id );

            // Update the metadata
            unlistable.Value = "bar";
            this.esu.SetUserMetadata( id, mlist );

            // Re-read the metadata
            MetadataList meta = this.esu.GetUserMetadata( id, null );
            Assert.AreEqual( "bar", meta.GetMetadata( "unlistable" ).Value, "value of 'unlistable' wrong" );
            
            // Check that content was not modified
            string content = Encoding.UTF8.GetString( this.esu.ReadObject( id, null, null ) );
            Assert.AreEqual( "hello", content, "object content wrong" );

        }

        /// <summary>
        /// Tests updating an object's ACL.
        /// </summary>
        [TestMethod()]
        public void testUpdateObjectAcl() {
            // Create an object with an ACL
            Acl acl = new Acl();
            acl.AddGrant( new Grant( new Grantee( getUid(esu.GetUid()), Grantee.GRANTEE_TYPE.USER ), Permission.FULL_CONTROL ) );
            Grant other = new Grant( Grantee.OTHER, Permission.READ );
            acl.AddGrant( other );
            ObjectId id = this.esu.CreateObject( acl, null, null, null );
            Assert.IsNotNull( id,"null ID returned" );
            cleanup.Add( id );

            // Read back the ACL and make sure it matches
            Acl newacl = this.esu.GetAcl( id );
            Debug.WriteLine( "Comparing " + newacl + " with " + acl );

            Assert.AreEqual( acl, newacl, "ACLs don't match" );
            
            // Change the ACL and update the object.
            acl.RemoveGrant( other );
            Grant o2 = new Grant( Grantee.OTHER, Permission.NONE );
            acl.AddGrant( o2 );
            this.esu.SetAcl( id, acl );
            
            // Read the ACL back and check it
            newacl = this.esu.GetAcl( id );
            Debug.WriteLine( "Comparing " + newacl + " with " + acl );
            Assert.AreEqual( acl, newacl, "ACLs don't match" );
        }

        /// <summary>
        /// Tests updating an object's contents
        /// </summary>
        [TestMethod()]
        public void testUpdateObjectContent() {
            // Create an object
            ObjectId id = this.esu.CreateObject( null, null, Encoding.UTF8.GetBytes( "hello" ), "text/plain" );
            Assert.IsNotNull( id,"null ID returned" );
            cleanup.Add( id );

            // Update part of the content
            Extent extent = new Extent( 1,1 );
            this.esu.UpdateObject( id, null, null, extent, Encoding.UTF8.GetBytes( "u" ), null ); 

            // Read back the content and check it
            string content = Encoding.UTF8.GetString( this.esu.ReadObject( id, null, null ) );
            Assert.AreEqual( "hullo", content, "object content wrong" );
        }

        /// <summary>
        /// Test replacing an object's entire contents
        /// </summary>
        [TestMethod()]
        public void testReplaceObjectContent() {
            // Create an object
            ObjectId id = this.esu.CreateObject( null, null, Encoding.UTF8.GetBytes( "hello" ), "text/plain" );
            Assert.IsNotNull( id,"null ID returned" );
            cleanup.Add( id );

            // Update all of the content
            this.esu.UpdateObject( id, null, null, null, Encoding.UTF8.GetBytes( "bonjour" ), null ); 

            // Read back the content and check it
            string content = Encoding.UTF8.GetString( this.esu.ReadObject( id, null, null ) );
            Assert.AreEqual( "bonjour", content, "object content wrong"  );
        }

        /// <summary>
        /// Test the UploadHelper's create method
        /// </summary>
        [TestMethod()]
        public void testCreateHelper()
        {
            // use a blocksize of 1 to test multiple transfers.
            UploadHelper uploadHelper = new UploadHelper(this.esu, new byte[1]);
            uploadHelper.ContentType = "text/plain";
            MemoryStream ms = new MemoryStream();
            ms.Write(Encoding.UTF8.GetBytes("hello"), 0, 5);

            ms.Seek(0, SeekOrigin.Begin);
            // Create an object from our file stream
            ObjectId id = uploadHelper.CreateObject(
                    ms,
                    null, null, true);
            cleanup.Add(id);

            // Read contents back and check them
            string content = Encoding.UTF8.GetString(this.esu.ReadObject(id, null, null));
            Assert.AreEqual("hello", content, "object content wrong");
        }

        /// <summary>
        /// Test the UploadHelper's create method
        /// </summary>
        [TestMethod()]
        public void testCreateHelperPath()
        {
            string dir = rand8char();
            string file = rand8char();
            ObjectPath op = new ObjectPath("/" + dir + "/" + file);

            // use a blocksize of 1 to test multiple transfers.
            UploadHelper uploadHelper = new UploadHelper(this.esu, new byte[1]);
            uploadHelper.ContentType = "text/plain";
            MemoryStream ms = new MemoryStream();
            ms.Write(Encoding.UTF8.GetBytes("hello"), 0, 5);

            ms.Seek(0, SeekOrigin.Begin);
            // Create an object from our file stream
            ObjectId id = uploadHelper.CreateObjectOnPath(
                    op,
                    ms,
                    null, null, true);
            cleanup.Add(id);

            // Read contents back and check them
            string content = Encoding.UTF8.GetString(this.esu.ReadObject(op, null, null));
            Assert.AreEqual("hello", content, "object content wrong");
        }

        /// <summary>
        /// Test the UploadHelper's update method
        /// </summary>
        [TestMethod()]
        public void testUpdateHelper()
        {
            // use a blocksize of 1 to test multiple transfers.
            UploadHelper uploadHelper = new UploadHelper(this.esu, new byte[1]);
            uploadHelper.ContentType = "text/plain";

            // Create an object with content.
            ObjectId id = this.esu.CreateObject(null, null, Encoding.UTF8.GetBytes("Four score and twenty years ago"), "text/plain");
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            // update the object contents
            MemoryStream ms = new MemoryStream();
            ms.Write(Encoding.UTF8.GetBytes("hello"), 0, 5);
            ms.Seek(0, SeekOrigin.Begin);

            uploadHelper.UpdateObject(id,
                    ms, null, null, true);

            // Read contents back and check them
            string content = Encoding.UTF8.GetString(this.esu.ReadObject(id, null, null));
            Assert.AreEqual("hello", content, "object content wrong");
        }

        /// <summary>
        /// Test the UploadHelper's update method
        /// </summary>
        [TestMethod()]
        public void testUpdateHelperPath()
        {
            string dir = rand8char();
            string file = rand8char();
            ObjectPath op = new ObjectPath("/" + dir + "/" + file);

            // use a blocksize of 1 to test multiple transfers.
            UploadHelper uploadHelper = new UploadHelper(this.esu, new byte[1]);
            uploadHelper.ContentType = "text/plain";

            // Create an object with content.
            ObjectId id = this.esu.CreateObjectOnPath(op, null, null, Encoding.UTF8.GetBytes("Four score and twenty years ago"), "text/plain");
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            // update the object contents
            MemoryStream ms = new MemoryStream();
            ms.Write(Encoding.UTF8.GetBytes("hello"), 0, 5);
            ms.Seek(0, SeekOrigin.Begin);

            uploadHelper.UpdateObject(op,
                    ms, null, null, true);

            // Read contents back and check them
            string content = Encoding.UTF8.GetString(this.esu.ReadObject(op, null, null));
            Assert.AreEqual("hello", content, "object content wrong");
        }

        /// <summary>
        /// Tests the download helper.  Tests both single and multiple requests.
        /// </summary>
        [TestMethod()]
        public void testDownloadHelper()
        {
            // Create an object with content.
            ObjectId id = this.esu.CreateObject(null, null, Encoding.UTF8.GetBytes("Four score and twenty years ago"), "text/plain");
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            // Download the content
            DownloadHelper downloadHelper = new DownloadHelper(this.esu, null);
            MemoryStream ms = new MemoryStream();
            downloadHelper.ReadObject(id, ms, false);
            ms.Close();

            // Check the download

            string data = Encoding.UTF8.GetString(ms.ToArray());
            Assert.AreEqual(data, "Four score and twenty years ago", "object content wrong");

            // Download again 1 byte in a request
            downloadHelper = new DownloadHelper(this.esu, new byte[1]);
            ms = new MemoryStream();
            downloadHelper.ReadObject(id, ms, false);
            ms.Close();

            // Check the download
            data = Encoding.UTF8.GetString(ms.ToArray());
            Assert.AreEqual("Four score and twenty years ago", data, "object content wrong");
        }

        /// <summary>
        /// Tests the download helper.  Tests both single and multiple requests.
        /// </summary>
        [TestMethod()]
        public void testDownloadHelperPath()
        {

            string dir = rand8char();
            string file = rand8char();
            ObjectPath op = new ObjectPath("/" + dir + "/" + file);

            // Create an object with content.
            ObjectId id = this.esu.CreateObjectOnPath( op, null, null, Encoding.UTF8.GetBytes("Four score and twenty years ago"), "text/plain");
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            // Download the content
            DownloadHelper downloadHelper = new DownloadHelper(this.esu, null);
            MemoryStream ms = new MemoryStream();
            downloadHelper.ReadObject(op, ms, false);
            ms.Close();

            // Check the download

            string data = Encoding.UTF8.GetString(ms.ToArray());
            Assert.AreEqual(data, "Four score and twenty years ago", "object content wrong");

            // Download again 1 byte in a request
            downloadHelper = new DownloadHelper(this.esu, new byte[1]);
            ms = new MemoryStream();
            downloadHelper.ReadObject(op, ms, false);
            ms.Close();

            // Check the download
            data = Encoding.UTF8.GetString(ms.ToArray());
            Assert.AreEqual("Four score and twenty years ago", data, "object content wrong");
        }

        [TestMethod()]
       	public void testListDirectory() {
		    string dir = rand8char();
		    string file = rand8char();
		    string dir2 = rand8char();
            ObjectPath dirPath = new ObjectPath( "/" + dir + "/" );
    	    ObjectPath op = new ObjectPath( "/" + dir + "/" + file );
    	    ObjectPath dirPath2 = new ObjectPath( "/" + dir + "/" + dir2 + "/" );
        	
    	    ObjectId dirId = this.esu.CreateObjectOnPath( dirPath, null, null, null, null );
            ObjectId id = this.esu.CreateObjectOnPath( op, null, null, null, null );
            this.esu.CreateObjectOnPath( dirPath2, null, null, null, null );
            cleanup.Add( op );
            cleanup.Add( dirPath2 );
            cleanup.Add( dirPath );
            Debug.WriteLine( "Path: " + op + " ID: " + id );
            Assert.IsNotNull( id );
            Assert.IsNotNull(dirId);

            // Read back the content
            string content = Encoding.UTF8.GetString( this.esu.ReadObject( op, null, null ) );
            Assert.AreEqual(  "", content, "object content wrong" );
            content = Encoding.UTF8.GetString( this.esu.ReadObject( id, null, null ) );
            Assert.AreEqual("", content, "object content wrong when reading by id");
            
            // List the parent path
            ListOptions options = new ListOptions();
            options.IncludeMetadata = false;
            List<DirectoryEntry> dirList = esu.ListDirectory( dirPath, options );
            Debug.WriteLine( "Dir content: " + content );
            Assert.IsTrue( directoryContains( dirList, op ), "File not found in directory" );
            Assert.IsTrue( directoryContains( dirList, dirPath2 ), "subdirectory not found in directory" );
	    }

        [TestMethod()]
        public void testListDirectoryPaged()
        {
            string dir = rand8char();
            string file = rand8char();
            string file2 = rand8char();
            string dir2 = rand8char();
            ObjectPath dirPath = new ObjectPath("/" + dir + "/");
            ObjectPath op = new ObjectPath("/" + dir + "/" + file);
            ObjectPath dirPath2 = new ObjectPath("/" + dir + "/" + dir2 + "/");
            ObjectPath op2 = new ObjectPath("/" + dir + "/" + file2);

            ObjectId dirId = this.esu.CreateObjectOnPath(dirPath, null, null, null, null);
            ObjectId id = this.esu.CreateObjectOnPath(op, null, null, null, null);
            this.esu.CreateObjectOnPath(dirPath2, null, null, null, null);
            this.esu.CreateObjectOnPath(op2, null, null, null, null);
            cleanup.Add(op);
            cleanup.Add(op2);
            cleanup.Add(dirPath2);
            cleanup.Add(dirPath);

            Debug.WriteLine("Path: " + op + " ID: " + id);
            Assert.IsNotNull(id);
            Assert.IsNotNull(dirId);

            // List the parent path
            ListOptions options = new ListOptions();
            options.IncludeMetadata = false;
            options.Limit = 1;
            List<DirectoryEntry> dirList = esu.ListDirectory(dirPath, options);
            // Iterate over token
            while (options.Token != null)
            {
                Debug.WriteLine("Continuing with token " + options.Token);
                dirList.AddRange(esu.ListDirectory(dirPath, options));
            }
            Assert.IsTrue(directoryContains(dirList, op), "File not found in directory");
            Assert.IsTrue(directoryContains(dirList, op2), "File2 not found in directory");
        }

        [TestMethod()]
        public void testListDirectoryWithMetadata()
        {
            // Create an object
            MetadataList mlist = new MetadataList();
            Metadata listable = new Metadata("listable", "foo", true);
            Metadata unlistable = new Metadata("unlistable", "bar", false);
            Metadata listable2 = new Metadata("listable2", "foo2 foo2", true);
            Metadata unlistable2 = new Metadata("unlistable2", "bar2 bar2", false);
            mlist.AddMetadata(listable);
            mlist.AddMetadata(unlistable);
            mlist.AddMetadata(listable2);
            mlist.AddMetadata(unlistable2);
            string dir = rand8char();
            string file = rand8char();
            ObjectPath dirPath = new ObjectPath("/" + dir + "/");
            ObjectPath op = new ObjectPath("/" + dir + "/" + file);

            ObjectId dirId = this.esu.CreateObjectOnPath(dirPath, null, null, null, null);
            ObjectId id = this.esu.CreateObjectOnPath(op, null, mlist, null, null);
            cleanup.Add(op);
            cleanup.Add(dirPath);
            Debug.WriteLine("Path: " + op + " ID: " + id);
            Assert.IsNotNull(id);
            Assert.IsNotNull(dirId);

            // Read back the content
            string content = Encoding.UTF8.GetString(this.esu.ReadObject(op, null, null));
            Assert.AreEqual("", content, "object content wrong");
            content = Encoding.UTF8.GetString(this.esu.ReadObject(id, null, null));
            Assert.AreEqual("", content, "object content wrong when reading by id");

            // List the parent path
            ListOptions options = new ListOptions();
            options.IncludeMetadata = true;
            //options.UserMetadata = new List<string>();
            //options.UserMetadata.Add("listable");
            List<DirectoryEntry> dirList = esu.ListDirectory(dirPath, options);
            Debug.WriteLine("Dir content: " + content);
            DirectoryEntry ent = findInDirectory(dirList, op);
            Assert.IsNotNull(ent, "File not found in directory");

            // Check metadata
            Assert.IsNotNull(ent.SystemMetadata.GetMetadata("size"), "Size missing from metadata");
            Assert.AreEqual("foo", ent.UserMetadata.GetMetadata("listable").Value, "Metadata value wrong");

            // listable2 should not be present
            //Assert.IsNull(ent.UserMetadata.GetMetadata("listable2"), "listable2 shouldn't be present");
        }

        private DirectoryEntry findInDirectory(List<DirectoryEntry> dir, ObjectPath path)
        {
            foreach (DirectoryEntry de in dir)
            {
                if (de.Path.Equals(path))
                {
                    return de;
                }
            }
            return null;
        }

	    private bool directoryContains( List<DirectoryEntry> dir, ObjectPath path ) {
		    foreach( DirectoryEntry de in dir ) {
			    if( de.Path.Equals( path ) ) {
				    return true;
			    }
		    }
    		
		    return false;
	    }
    	
	    /**
	     * This method tests various legal and illegal pathnames
	     * @throws Exception
	     */
        [TestMethod()]
	    public void testPathNaming() {
		    ObjectPath path = new ObjectPath( "/some/file" );
            Assert.IsFalse(path.IsDirectory(), "File should not be directory");
		    path = new ObjectPath( "/some/file.txt" );
            Assert.IsFalse(path.IsDirectory(), "File should not be directory");
            ObjectPath path2 = new ObjectPath("/some/file.txt");
		    Assert.AreEqual( path, path2, "Equal paths should be equal" );
    		
		    path = new ObjectPath( "/some/file/with/long.path/extra.stuff.here.zip" );
            Assert.IsFalse(path.IsDirectory(), "File should not be directory");
    		
		    path = new ObjectPath( "/" );
            Assert.IsTrue(path.IsDirectory(), "File should be directory");
    		
		    path = new ObjectPath( "/long/path/with/lots/of/elements/" );
            Assert.IsTrue(path.IsDirectory(), "File should be directory");
    		
	    }
    	
	    /**
	     * Tests the 'get all metadata' call using a path
	     * @param uid
	     * @throws Exception
	     */
        [TestMethod()]
	    public void testGetAllMetadataByPath() {
    	    ObjectPath op = new ObjectPath( "/" + rand8char() + ".tmp" );
            // Create an object with an ACL
            Acl acl = new Acl();
            acl.AddGrant(new Grant(new Grantee(esu.GetUid(), Grantee.GRANTEE_TYPE.USER), Permission.FULL_CONTROL));
            acl.AddGrant( new Grant( Grantee.OTHER, Permission.READ ) );
            MetadataList mlist = new MetadataList();
            Metadata listable = new Metadata( "listable", "foo", true );
            Metadata unlistable = new Metadata( "unlistable", "bar", false );
            Metadata listable2 = new Metadata( "listable2", "foo2 foo2", true );
            Metadata unlistable2 = new Metadata( "unlistable2", "bar2 bar2", false );
            mlist.AddMetadata( listable );
            mlist.AddMetadata( unlistable );
            mlist.AddMetadata( listable2 );
            mlist.AddMetadata( unlistable2 );

            ObjectId id = this.esu.CreateObjectOnPath( op, acl, null, null, null );
            this.esu.UpdateObject( op, null, mlist, null, null, null );
            Assert.IsNotNull( id, "null ID returned" );
            cleanup.Add( op );
            
            // Read it back with HEAD call
            ObjectMetadata om = this.esu.GetAllMetadata( op );
            Assert.IsNotNull( om.Metadata.GetMetadata( "listable" ), "value of 'listable' missing" );
            Assert.IsNotNull( om.Metadata.GetMetadata("unlistable"), "value of 'unlistable' missing" );
            Assert.IsNotNull( om.Metadata.GetMetadata("atime"), "value of 'atime' missing" );
            Assert.IsNotNull( om.Metadata.GetMetadata("ctime"), "value of 'ctime' missing" );
            Assert.AreEqual( "foo", om.Metadata.GetMetadata("listable").Value, "value of 'listable' wrong" );
            Assert.AreEqual( "bar", om.Metadata.GetMetadata("unlistable").Value, "value of 'unlistable' wrong" );

            // Check the ACL
            // not checking this by path because an extra groupid is added 
            // during the create calls by path.
            //Assert.AreEqual( acl, om.getAcl(), "ACLs don't match" );

	    }
    	
        [TestMethod()]
	    public void testGetAllMetadataById() {
            // Create an object with an ACL
            Acl acl = new Acl();
            acl.AddGrant(new Grant(new Grantee(getUid(esu.GetUid()), Grantee.GRANTEE_TYPE.USER), Permission.FULL_CONTROL));
            acl.AddGrant( new Grant( Grantee.OTHER, Permission.READ ) );
            MetadataList mlist = new MetadataList();
            Metadata listable = new Metadata( "listable", "foo", true );
            Metadata unlistable = new Metadata( "unlistable", "bar", false );
            Metadata listable2 = new Metadata( "listable2", "foo2 foo2", true );
            Metadata unlistable2 = new Metadata( "unlistable2", "bar2 bar2", false );
            mlist.AddMetadata(listable);
            mlist.AddMetadata(unlistable);
            mlist.AddMetadata(listable2);
            mlist.AddMetadata(unlistable2);

            ObjectId id = this.esu.CreateObject( acl, mlist, null, null );
            Assert.IsNotNull( id, "null ID returned" );
            cleanup.Add( id );
            
            // Read it back with HEAD call
            ObjectMetadata om = this.esu.GetAllMetadata( id );
            Assert.IsNotNull(om.Metadata.GetMetadata("listable"), "value of 'listable' missing");
            Assert.IsNotNull(om.Metadata.GetMetadata("unlistable"), "value of 'unlistable' missing");
            Assert.IsNotNull(om.Metadata.GetMetadata("atime"), "value of 'atime' missing");
            Assert.IsNotNull(om.Metadata.GetMetadata("ctime"), "value of 'ctime' missing");
            Assert.AreEqual("foo", om.Metadata.GetMetadata("listable").Value, "value of 'listable' wrong");
            Assert.AreEqual("bar", om.Metadata.GetMetadata("unlistable").Value, "value of 'unlistable' wrong");

            // Check the ACL
            Assert.AreEqual( acl, om.ACL, "ACLs don't match" );
    		
	    }

        ///**
        // * Tests getting object replica information.
        // */
        //[TestMethod()]
        //public void testGetObjectReplicaInfo() {
        //    ObjectId id = this.esu.CreateObject( null, null, Encoding.UTF8.GetBytes( "hello" ), "text/plain" );
        //    Assert.IsNotNull(id, "null ID returned");
        //    cleanup.Add(id);
            
        //    MetadataTags mt = new MetadataTags();
        //    mt.AddTag( new MetadataTag( "user.maui.lso", false ) );
        //    MetadataList meta = this.esu.GetUserMetadata( id, mt );
        //    Assert.IsNotNull( meta.GetMetadata( "user.maui.lso" ) );
        //    Debug.WriteLine( "Replica info: " + meta.GetMetadata( "user.maui.lso" ) );
        //}

        [TestMethod()]
        public void testGetShareableUrl()
        {
            // Create an object with content.
            string str = "Four score and twenty years ago";
            ObjectId id = this.esu.CreateObject(null, null, Encoding.UTF8.GetBytes(str), "text/plain");
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            DateTime expiration = DateTime.UtcNow;
            expiration += TimeSpan.FromHours(5);
            Uri u = esu.GetShareableUrl(id, expiration);

            Debug.WriteLine("Sharable URL: " + u);

            WebRequest wr = WebRequest.Create(u);
            Stream s = wr.GetResponse().GetResponseStream();
            StreamReader sr = new StreamReader(s);
            string content = sr.ReadToEnd();
            sr.Close();
            Debug.WriteLine("Content: " + content);
            Assert.AreEqual(str, content, "URL does not contain proper content");
        }

        [TestMethod()]
        public void testGetShareableUrlOnPath()
        {
            // Create an object with content.
            string str = "Four score and twenty years ago";
            ObjectPath op = new ObjectPath("/" + rand8char() + ".txt");
            ObjectId id = this.esu.CreateObjectOnPath(op, null, null, Encoding.UTF8.GetBytes(str), "text/plain");
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(op);

            DateTime expiration = DateTime.UtcNow;
            expiration += TimeSpan.FromHours(5);
            string disposition = "attachment; filename=\"no UTF support.txt\"; filename*=UTF-8''" + Uri.EscapeDataString("бöｼ.txt");
            Uri u = esu.GetShareableUrl(id, expiration, disposition);

            Debug.WriteLine("Sharable URL: " + u);

            WebRequest wr = WebRequest.Create(u);
            WebResponse resp = wr.GetResponse();
            Stream s = resp.GetResponseStream();
            StreamReader sr = new StreamReader(s);
            string content = sr.ReadToEnd();
            sr.Close();
            Debug.WriteLine("Content: " + content);
            Assert.AreEqual(disposition, resp.Headers["Content-Disposition"]);
            Assert.AreEqual(str, content, "URL does not contain proper content");
        }


        [TestMethod()]
        public void testUploadDownload() {
            // Create a byte array to test
            int size=10*1024*1024;
            byte[] testData = new byte[size];
            for( int i=0; i<size; i++ ) {
                testData[i] = (byte)(i%0x93);
            }
            UploadHelper uh = new UploadHelper( this.esu, null );
            MemoryStream ms = new MemoryStream();
            ms.Write(testData, 0, testData.Length);
            ms.Seek(0, SeekOrigin.Begin);

            ObjectId id = uh.CreateObject(ms, null, null, true);
            cleanup.Add( id );
            
            MemoryStream baos = new MemoryStream();
            
            DownloadHelper dl = new DownloadHelper( this.esu, new byte[4*1024*1024] );
            dl.ReadObject( id, baos, true );
            
            Assert.IsFalse( dl.Failed, "Download should have been OK" );
            Assert.IsNull( dl.Error, "Error should have been null" );

            byte[] outData = baos.ToArray();
            
            // Check the files
            Assert.AreEqual( testData.Length, outData.Length, "File lengths differ" );

            for (int i = 0; i < testData.Length; i++)
            {
                Assert.AreEqual(testData[i], outData[i], "Arrays differ at offset " + i);
            }
        
        }

        [TestMethod()]
        public void testChecksums()
        {
            // Create a byte array to test
            int size = 10 * 1024 * 1024;
            byte[] testData = new byte[size];
            for (int i = 0; i < size; i++)
            {
                testData[i] = (byte)(i % 0x93);
            }
            UploadHelper uh = new UploadHelper(this.esu, null);
            uh.ComputeChecksums = true;
            MemoryStream ms = new MemoryStream();
            ms.Write(testData, 0, testData.Length);
            ms.Seek(0, SeekOrigin.Begin);

            ObjectId id = uh.CreateObject(ms, null, null, true);
            cleanup.Add(id);

            MemoryStream baos = new MemoryStream();

            DownloadHelper dl = new DownloadHelper(this.esu, new byte[4 * 1024 * 1024]);
            dl.Checksumming = true;
            dl.ReadObject(id, baos, true);

            Assert.IsFalse(dl.Failed, "Download should have been OK");
            Assert.IsNull(dl.Error, "Error should have been null");

            byte[] outData = baos.ToArray();

            // Check the files
            Assert.AreEqual(testData.Length, outData.Length, "File lengths differ");

            for (int i = 0; i < testData.Length; i++)
            {
                Assert.AreEqual(testData[i], outData[i], "Arrays differ at offset " + i);
            }
        }

        [TestMethod()]
        public void testAppendChecksum()
        {
            Checksum ck = new Checksum(Checksum.Algorithm.SHA0);
            ObjectId id = esu.CreateObject(null, null, Encoding.UTF8.GetBytes("hello"), "text/plain", ck);
            cleanup.Add(id);

            esu.UpdateObject(id, null, null, new Extent(5, 6), Encoding.UTF8.GetBytes(" world"), null, ck);
            

        }

        /// <summary>
        /// Test renaming files
        /// </summary>
        [TestMethod()]
        public void testRename()
        {
            ObjectPath op1 = new ObjectPath("/" + rand8char() + ".tmp");
            ObjectPath op2 = new ObjectPath("/" + rand8char() + ".tmp");
            ObjectPath op3 = new ObjectPath("/" + rand8char() + ".tmp");
            ObjectPath op4 = new ObjectPath("/" + rand8char() + ".tmp");
            ObjectId id = this.esu.CreateObjectOnPath(op1, null, null, 
                Encoding.UTF8.GetBytes("Four score and seven years ago"), "text/plain");
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            // Rename the object
            this.esu.Rename(op1, op2, false);

            // Read back the content
            string content = Encoding.UTF8.GetString(this.esu.ReadObject(op2, null, null));
            Assert.AreEqual("Four score and seven years ago", content, "object content wrong");

            // Attempt overwrite
            id = this.esu.CreateObjectOnPath(op3, null, null,
                Encoding.UTF8.GetBytes("Four score and seven years ago"), "text/plain");
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);
            id = this.esu.CreateObjectOnPath(op4, null, null,
                Encoding.UTF8.GetBytes("You shouldn't see me"), "text/plain");
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);
            this.esu.Rename(op3, op4, true);

            // Wait for rename to complete
            System.Threading.Thread.Sleep(5000);

            // Read back the content
            content = Encoding.UTF8.GetString(this.esu.ReadObject(op4, null, null));
            Assert.AreEqual("Four score and seven years ago", content, "object content wrong (3)");
        }

        [TestMethod()]
        public void testGetServiceInformation()
        {
            ServiceInformation si = this.esu.GetServiceInformation();

            Assert.IsNotNull(si.AtmosVersion);
            Debug.WriteLine("Atmos " + si.AtmosVersion);
        }

        [TestMethod()]
        public void testGetServiceInformationFeatures() {
            ServiceInformation info = this.esu.GetServiceInformation();
            string featureString = "";
            foreach (string feature in info.Features) { featureString += feature + ", "; }
            Debug.WriteLine( "Supported features: " + featureString.Substring(0, featureString.Length - 2) );

            Assert.IsTrue(info.Features.Count() > 0, "Expected at least one feature");
        }

        [TestMethod()]
	    public void testCreateChecksum() {
		    Checksum ck = new Checksum( EsuApiLib.Checksum.Algorithm.SHA0 );
            ObjectId id = this.esu.CreateObject( null, null, Encoding.UTF8.GetBytes("hello"), "text/plain", ck );
		    Debug.WriteLine( "Checksum: " + ck );
		    cleanup.Add( id );
	    }

        [TestMethod()]
        public void testReadChecksum()
        {
            Checksum ck = new Checksum(EsuApiLib.Checksum.Algorithm.SHA0);
            ObjectId id = this.esu.CreateObject(null, null, Encoding.UTF8.GetBytes("Four score and seven years ago"), "text/plain", ck);
            Debug.WriteLine("Checksum: " + ck);
            cleanup.Add(id);

            // Read back.
            Checksum ck2 = new Checksum(Checksum.Algorithm.SHA0);
            string content = Encoding.UTF8.GetString(this.esu.ReadObject(id, null, null, ck2));
            Assert.AreEqual("Four score and seven years ago", content, "object content wrong");

        }

        [TestMethod()]
        public void testGetObjectInfo()
        {
            MetadataList mlist = new MetadataList();
            Metadata policy = new Metadata("policy", "retaindelete", false);
            mlist.AddMetadata(policy);
            ObjectId id = this.esu.CreateObject(null, mlist, Encoding.UTF8.GetBytes("hello"), "text/plain");
            Assert.IsNotNull(id, "null ID returned");
            cleanup.Add(id);

            // Read back the content
            string content = Encoding.UTF8.GetString(this.esu.ReadObject(id, null, null));
            Assert.AreEqual("hello", content, "object content wrong");

            ObjectInfo info = this.esu.GetObjectInfo(id);
            Assert.IsNotNull(info, "ObjectInfo null");
            Assert.IsNotNull(info.ObjectID, "ObjectId null");
            Assert.IsNotNull(info.RawXml, "Raw XML null");
            Assert.IsNotNull(info.Replicas, "Replicas is null");
            Assert.IsTrue(info.Replicas.Count > 0, "Zero replicas found");
            Assert.IsNotNull(info.Retention, "No retention information");
            Assert.IsNotNull(info.Expiration, "Expiration null");
            Assert.IsNotNull(info.Selection, "Selection null");

            Debug.WriteLine("Expires: " + info.Expiration.EndAt);
            Debug.WriteLine("Retention ends: " + info.Retention.EndAt);
        }

        private string rand8char() {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 8; i++) {
                sb.Append((char)('a' + rand.Next( 26 )) );
            }
            return sb.ToString();
        }

        [TestMethod()]
        public void testObjectKeyCRUD()
        {
            ObjectKey key = new ObjectKey("Test_key-pool#@!$%^..", "KEY_TEST");
            try
            {
                string content = "Hello World!";
                byte[] data = Encoding.UTF8.GetBytes(content);

                ObjectId oid = this.esu.CreateObjectWithKey(key, null, null, data, "text/plain");

                Assert.IsNotNull(oid, "Null object ID returned");

                string readContent = Encoding.UTF8.GetString(this.esu.ReadObject(key, null, null));
                Assert.AreEqual(content, readContent, "content mismatch");

                content = "Hello Waldo!";
                data = Encoding.UTF8.GetBytes(content);
                this.esu.UpdateObject(key, null, null, null, data, null);

                readContent = Encoding.UTF8.GetString(this.esu.ReadObject(key, null, null));
                Assert.AreEqual(content, readContent, "content mismatch");

            }
            finally
            {
                this.esu.DeleteObject(key);
            }

            try
            {
                this.esu.ReadObject(key, null, null);
                Assert.Fail("Object still exists");
            }
            catch (EsuException e)
            {
                if (e.Code != 1003) throw e;
            }
        }

        [TestMethod()]
        public void testKeysOther()
        {
            ObjectKey key = new ObjectKey("Test_key-pool#@!$%^..", "KEY_TEST2");
            string content = "Key tests!";
            byte[] data = Encoding.UTF8.GetBytes(content);

            ObjectId oid = this.esu.CreateObjectWithKey(key, null, null, data, "text/plain");
            cleanup.Add(oid);

            // test getting stuff
            Acl acl = this.esu.GetAcl(key);
            Assert.IsNotNull(acl, "ACL is null");
            ObjectMetadata objectMeta = this.esu.GetAllMetadata(key);
            Assert.IsTrue(objectMeta.Metadata.Count() > 0, "Object metadata is null");
            ObjectInfo info = this.esu.GetObjectInfo(key);
            Assert.IsNotNull(info.ObjectID, "GetObjectInfo object ID is null");
            Assert.IsTrue(this.esu.GetSystemMetadata(key, null).Count() > 0, "System Metadata is empty");
            Assert.IsTrue(this.esu.GetUserMetadata(key, null).Count() == 0, "User Metadata is not empty");

            // test setting stuff
            foreach (Grant grant in acl) {
                if (grant.Grantee.Name == "other")
                {
                    grant.Permission = Permission.READ;
                    break;
                }
            }
            this.esu.SetAcl(key, acl);
            Assert.AreEqual(acl, this.esu.GetAcl(key), "ACL is different");
            MetadataList mList = new MetadataList();
            mList.AddMetadata(new Metadata("foo", "bar", false));
            mList.AddMetadata(new Metadata("listable", "", true));
            this.esu.SetUserMetadata(key, mList);
            MetadataList readList = this.esu.GetUserMetadata(key, null);
            Assert.AreEqual(mList.ToString(), readList.ToString(), "User meta is different");
        }

        [TestMethod()]
        public void testReadAccessToken()
        {
            string dir = rand8char();
            string file = rand8char();
            ObjectPath op = new ObjectPath("/" + dir + "/" + file);
            string data = "hello";
            ObjectId id = esu.CreateObjectOnPath(op, null, null, Encoding.UTF8.GetBytes(data), "text/plain");

            DateTime expiration = DateTime.UtcNow;
            expiration += TimeSpan.FromMinutes(5);

            SourceType source = new SourceType();
            source.Allow = new string[] {"10.0.0.0/8", "128.0.0.0/8"};
            source.Disallow = new string[] {"1.1.1.1"};

            ContentLengthRangeType range = new ContentLengthRangeType();
            range.From = 0;
            range.To = 1024; // 1KB

            FormFieldType formField1 = new FormFieldType();
            formField1.Name = "x-emc-meta";
            formField1.Optional = true;
            FormFieldType formField2 = new FormFieldType();
            formField2.Name = "x-emc-listable-meta";
            formField2.Optional = true;

            PolicyType policy = new PolicyType();
            policy.Expiration = expiration;
            policy.Source = source;
            policy.MaxDownloads = 2;
            policy.MaxUploads = 0;
            policy.ContentLengthRange = range;
            policy.FormField = new FormFieldType[] {formField1, formField2};

            Uri tokenUri = esu.CreateAccessToken(id, policy, null);

            WebResponse response = WebRequest.Create(tokenUri).GetResponse();
            string content = new StreamReader(response.GetResponseStream()).ReadToEnd();
            response.Close();
            Assert.AreEqual(data, content, "Token URL does not contain proper content");

            esu.DeleteAccessToken(tokenUri);

            tokenUri = esu.CreateAccessToken(op, policy, null);

            response = WebRequest.Create(tokenUri).GetResponse();
            content = new StreamReader(response.GetResponseStream()).ReadToEnd();
            response.Close();
            Assert.AreEqual(data, content, "Token URL does not contain proper content");

            policy.MaxDownloads = policy.MaxDownloads - 1; // we already used one

            ListOptions options = new ListOptions();
            List<AccessTokenType> tokens = esu.ListAccessTokens(options);
            Assert.AreEqual(1, tokens.Count, "ListTokens returns wrong count");
            AssertTokenPolicy(tokens[0], policy);

            AccessTokenType token = esu.GetAccessToken(tokenUri);

            esu.DeleteAccessToken(token.AccessTokenId);

            string path = tokenUri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            Assert.AreEqual(path.Split('/').Last(), token.AccessTokenId, "token ID doesn't match");
            AssertTokenPolicy(token, policy);
        }

        [TestMethod()]
        public void testWriteAccessToken()
        {
            string dir = rand8char();
            string file = rand8char();
            ObjectPath op = new ObjectPath("/" + dir + "/" + file);
            esu.CreateObjectOnPath(new ObjectPath("/" + dir + "/"), null, null, null, null);

            DateTime expiration = DateTime.UtcNow;
            expiration += TimeSpan.FromMinutes(5);

            SourceType source = new SourceType();
            source.Allow = new string[] { "10.0.0.0/8", "128.0.0.0/8" };
            source.Disallow = new string[] { "1.1.1.1" };

            ContentLengthRangeType range = new ContentLengthRangeType();
            range.From = 0;
            range.To = 1024; // 1KB

            FormFieldType formField1 = new FormFieldType();
            formField1.Name = "x-emc-meta";
            formField1.Optional = true;
            FormFieldType formField2 = new FormFieldType();
            formField2.Name = "x-emc-listable-meta";
            formField2.Optional = true;

            PolicyType policy = new PolicyType();
            policy.Expiration = expiration;
            policy.Source = source;
            policy.MaxDownloads = 2;
            policy.MaxUploads = 1;
            policy.ContentLengthRange = range;
            policy.FormField = new FormFieldType[] { formField1, formField2 };

            Uri tokenUri = esu.CreateAccessToken(op, policy, null);

            // create upload form manually (easiest since we're using HttpWebRequest)
            string content = "Form Upload Test";
            string boundary = "BOUNDARY_1234567890";
            string EOL = "\r\n";
            string payload = "--" + boundary + EOL;
            payload += "Content-Type: text/plain" + EOL;
            payload += "Content-Disposition: form-data; name=\"x-emc-meta\"" + EOL + EOL;
            payload += "color=gray,size=3,foo=bar";
            payload += EOL + "--" + boundary + EOL;
            payload += "Content-Type: text/plain" + EOL;
            payload += "Content-Disposition: form-data; name=\"x-emc-listable-meta\"" + EOL + EOL;
            payload += "listable=";
            payload += EOL + "--" + boundary + EOL;
            payload += "Content-Type: text/plain" + EOL;
            payload += "Content-Disposition: form-data; name=\"data\"; filename=\"foo.txt\"" + EOL + EOL;
            payload += content;
            payload += EOL + "--" + boundary + "--" + EOL;

            WebRequest request = WebRequest.Create(tokenUri);
            request.Method = "POST";
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            new MemoryStream(Encoding.UTF8.GetBytes(payload)).CopyTo(request.GetRequestStream());
            WebResponse response = request.GetResponse();
            Assert.AreEqual(201, (int) (response as HttpWebResponse).StatusCode, "Wrong status code");
            string path = response.Headers["Location"];
            ObjectId oid = new ObjectId(path.Split('/').Last());
            cleanup.Add(oid);
            response.Close();

            // read back object via token
            response = WebRequest.Create(tokenUri).GetResponse();
            Assert.AreEqual(content, new StreamReader(response.GetResponseStream()).ReadToEnd(), "content from token not equal");
            response.Close();

            // read back object via namespace
            Assert.AreEqual(content, Encoding.UTF8.GetString(esu.ReadObject(op, null, null)), "content from namespace not equal");

            esu.DeleteAccessToken(tokenUri);
        }

        [TestMethod()]
        public void testUnicodeMetadata() {
            MetadataList mlist = new MetadataList();
            Metadata nbspValue = new Metadata( "nbspvalue", "Nobreak\u00A0Value", false );
            Metadata nbspName = new Metadata( "Nobreak\u00A0Name", "regular text here", false );
            Metadata cryllic = new Metadata( "cryllic", "спасибо", false );
            Debug.WriteLine( "NBSP Value: " + nbspValue );
            Debug.WriteLine( "NBSP Name: " + nbspName );

            mlist.AddMetadata( nbspValue );
            mlist.AddMetadata( nbspName );
            mlist.AddMetadata( cryllic );

            ObjectId id = this.esu.CreateObject( null, mlist, null, null );
            Assert.IsNotNull( id, "null ID returned" );
            cleanup.Add( id );

            // Read and validate the metadata
            MetadataList meta = this.esu.GetUserMetadata( id, null );
            Debug.WriteLine( "Read Back:" );
            Debug.WriteLine( "NBSP Value: " + meta.GetMetadata( "nbspvalue" ) );
            Debug.WriteLine( "NBSP Name: " + meta.GetMetadata( "Nobreak\u00A0Name" ) );
            Assert.AreEqual( "Nobreak\u00A0Value", meta.GetMetadata( "nbspvalue" ).Value, "value of 'nobreakvalue' wrong" );
            Assert.AreEqual( "спасибо", meta.GetMetadata( "cryllic" ).Value, "Value of cryllic wrong" );
        }

        [TestMethod()]
        public void testUnicodePath() {
            String dirName = rand8char();
            ObjectPath path = new ObjectPath( "/" + dirName + "/бöｼ.txt" );
            ObjectId id = this.esu.CreateObjectOnPath( path, null, null, null, null );
            Assert.IsNotNull( id, "null ID returned" );
            cleanup.Add( id );

            ObjectPath parent = new ObjectPath( "/" + dirName + "/" );
            List<DirectoryEntry> ents = this.esu.ListDirectory( parent, null );
            bool found = false;
            foreach ( DirectoryEntry ent in ents ) {
                if ( ent.Path.Equals( path ) ) {
                    found = true;
                }
            }
            Assert.IsTrue( found, "Did not find unicode file in dir" );

            // Check read
            this.esu.ReadObject( path, null, null );

        }

        [TestMethod()]
        public void testUtf8Metadata() {
            String oneByteCharacters = "Hello! ";
            String twoByteCharacters = "\u0410\u0411\u0412\u0413"; // Cyrillic letters
            String fourByteCharacters = "\ud841\udf0e\ud841\udf31\ud841\udf79\ud843\udc53"; // Chinese symbols
            String utf8String = oneByteCharacters + twoByteCharacters + fourByteCharacters;

            MetadataList metaList = new MetadataList();
            metaList.AddMetadata( new Metadata( "utf8Key", utf8String, false ) );
            metaList.AddMetadata( new Metadata( utf8String, "utf8Value", false ) );

            ObjectId id = this.esu.CreateObject( null, metaList, null, null );
            cleanup.Add( id );

            // list all tags and make sure the UTF8 tag is in the list
            MetadataTags tags = this.esu.ListUserMetadataTags( id );
            Assert.IsTrue( tags.Contains( utf8String ), "UTF8 key not found in tag list" );

            // get the user metadata and make sure all UTF8 characters are accurate
            metaList = this.esu.GetUserMetadata( id, null );
            Metadata meta = metaList.GetMetadata( utf8String );
            Assert.AreEqual( meta.Name, utf8String, "UTF8 key does not match" );
            Assert.AreEqual( meta.Value, "utf8Value", "UTF8 key value does not match" );
            Assert.AreEqual( metaList.GetMetadata( "utf8Key" ).Value, utf8String, "UTF8 value does not match" );

            // test set metadata with UTF8
            metaList = new MetadataList();
            metaList.AddMetadata( new Metadata( "newKey", utf8String + "2", false ) );
            metaList.AddMetadata( new Metadata( utf8String + "2", "newValue", false ) );
            this.esu.SetUserMetadata( id, metaList );

            // verify set metadata call (also testing getAllMetadata)
            ObjectMetadata objMeta = this.esu.GetAllMetadata( id );
            metaList = objMeta.Metadata;
            meta = metaList.GetMetadata( utf8String + "2" );
            Assert.AreEqual( meta.Name, utf8String + "2", "UTF8 key does not match" );
            Assert.AreEqual( meta.Value, "newValue", "UTF8 key value does not match" );
            Assert.AreEqual( metaList.GetMetadata( "newKey" ).Value, utf8String + "2", "UTF8 value does not match" );
        }

        [TestMethod()]
        public void testUtf8MetadataFilter() {
            String oneByteCharacters = "Hello! ";
            String twoByteCharacters = "\u0410\u0411\u0412\u0413"; // Cyrillic letters
            String fourByteCharacters = "\ud841\udf0e\ud841\udf31\ud841\udf79\ud843\udc53"; // Chinese symbols
            String utf8String = oneByteCharacters + twoByteCharacters + fourByteCharacters;

            MetadataList metaList = new MetadataList();
            metaList.AddMetadata( new Metadata( "utf8Key", utf8String, false ) );
            metaList.AddMetadata( new Metadata( utf8String, "utf8Value", false ) );

            ObjectId id = this.esu.CreateObject( null, metaList, null, null );
            cleanup.Add( id );

            // apply a filter that includes the UTF8 tag
            MetadataTags tags = new MetadataTags();
            tags.AddTag( new MetadataTag( utf8String, false ) );
            metaList = this.esu.GetUserMetadata( id, tags );
            Assert.AreEqual( metaList.Count(), 1, "UTF8 filter was not honored" );
            Assert.IsNotNull( metaList.GetMetadata( utf8String ), "UTF8 key was not found in filtered results" );
        }

        [TestMethod()]
        public void testUtf8DeleteMetadata() {
            String oneByteCharacters = "Hello! ";
            String twoByteCharacters = "\u0410\u0411\u0412\u0413"; // Cyrillic letters
            String fourByteCharacters = "\ud841\udf0e\ud841\udf31\ud841\udf79\ud843\udc53"; // Chinese symbols
            String utf8String = oneByteCharacters + twoByteCharacters + fourByteCharacters;

            MetadataList metaList = new MetadataList();
            metaList.AddMetadata( new Metadata( "utf8Key", utf8String, false ) );
            metaList.AddMetadata( new Metadata( utf8String, "utf8Value", false ) );

            ObjectId id = this.esu.CreateObject( null, metaList, null, null );
            cleanup.Add( id );

            // delete the UTF8 tag
            MetadataTags tags = new MetadataTags();
            tags.AddTag( new MetadataTag( utf8String, false ) );
            this.esu.DeleteUserMetadata( id, tags );

            // verify delete was successful
            tags = this.esu.ListUserMetadataTags( id );
            Assert.IsFalse( tags.Contains( utf8String ), "UTF8 key was not deleted" );
        }

        [TestMethod()]
        public void testUtf8ListableMetadata() {
            String oneByteCharacters = "Hello! ";
            String twoByteCharacters = "\u0410\u0411\u0412\u0413"; // Cyrillic letters
            String fourByteCharacters = "\ud841\udf0e\ud841\udf31\ud841\udf79\ud843\udc53"; // Chinese symbols
            String utf8String = oneByteCharacters + twoByteCharacters + fourByteCharacters;

            MetadataList metaList = new MetadataList();
            metaList.AddMetadata( new Metadata( utf8String, "utf8Value", true ) );

            ObjectId id = this.esu.CreateObject( null, metaList, null, null );
            cleanup.Add( id );

            metaList = this.esu.GetUserMetadata( id, null );
            Metadata meta = metaList.GetMetadata( utf8String );
            Assert.AreEqual( meta.Name, utf8String, "UTF8 key does not match" );
            Assert.AreEqual( meta.Value, "utf8Value", "UTF8 key value does not match" );
            Assert.IsTrue( meta.Listable, "UTF8 metadata is not listable" );

            // verify we can list the tag and see our object
            bool found = false;
            foreach ( ObjectResult result in this.esu.ListObjects( utf8String, null ) ) {
                if ( result.Id.Equals( id ) ) {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue( found, "UTF8 tag listing did not contain the correct object ID" );

            // verify we can list child tags of the UTF8 tag
            MetadataTags tags = this.esu.GetListableTags( new MetadataTag( utf8String, true ) );
            Assert.IsNotNull( tags, "UTF8 child tag listing was null" );
        }

        [TestMethod()]
        public void testUtf8ListableTagWithComma() {
            String stringWithComma = "Hello, you!";

            MetadataList metaList = new MetadataList();
            metaList.AddMetadata( new Metadata( stringWithComma, "value", true ) );

            ObjectId id = this.esu.CreateObject( null, metaList, null, null );
            cleanup.Add( id );

            metaList = this.esu.GetUserMetadata( id, null );
            Metadata meta = metaList.GetMetadata( stringWithComma );
            Assert.AreEqual( meta.Name, stringWithComma, "key does not match" );
            Assert.IsTrue( meta.Listable, "metadata is not listable" );

            bool found = false;
            foreach ( ObjectResult result in this.esu.ListObjects( stringWithComma, null ) ) {
                if ( result.Id.Equals( id ) ) {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue( found, "listing did not contain the correct object ID" );
        }

        [TestMethod()]
        public void testUtf8Path() {
            String oneByteCharacters = "Hello! ,";
            String twoByteCharacters = "\u0410\u0411\u0412\u0413"; // Cyrillic letters
            String fourByteCharacters = "\ud841\udf0e\ud841\udf31\ud841\udf79\ud843\udc53"; // Chinese symbols
            String crazyName = oneByteCharacters + twoByteCharacters + fourByteCharacters;
            byte[] content = Encoding.UTF8.GetBytes( "Crazy name creation test." );
            ObjectPath path = new ObjectPath( TESTDIR + crazyName );

            // create crazy-name object
            this.esu.CreateObjectOnPath( path, null, null, content, "text/plain" );

            cleanup.Add(path);

            // verify name in directory list
            bool found = false;
            foreach ( DirectoryEntry entry in this.esu.ListDirectory( new ObjectPath( TESTDIR ), null ) ) {
                if ( entry.Path.ToString().Equals( path.ToString() ) ) {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue( found, "crazyName not found in directory listing" );

            // verify content
            Assert.IsTrue( content.SequenceEqual( this.esu.ReadObject( path, null, null ) ), "content does not match" );
        }

        [TestMethod()]
        public void testUtf8Content() {
            String oneByteCharacters = "Hello! ,";
            String twoByteCharacters = "\u0410\u0411\u0412\u0413"; // Cyrillic letters
            String fourByteCharacters = "\ud841\udf0e\ud841\udf31\ud841\udf79\ud843\udc53"; // Chinese symbols
            byte[] content = Encoding.UTF8.GetBytes(oneByteCharacters + twoByteCharacters + fourByteCharacters);
            ObjectPath path = new ObjectPath( TESTDIR + "utf8Content.txt" );

            // create object with multi-byte UTF-8 content
            this.esu.CreateObjectOnPath( path, null, null, content, "text/plain" );

            // verify content
            Assert.IsTrue( content.SequenceEqual( this.esu.ReadObject( path, null, null ) ), "content does not match" );
        }

        [TestMethod()]
        public void testUtf8Rename() {
            String oneByteCharacters = "Hello! ,";
            String twoByteCharacters = "\u0410\u0411\u0412\u0413"; // Cyrillic letters
            String fourByteCharacters = "\ud841\udf0e\ud841\udf31\ud841\udf79\ud843\udc53"; // Chinese symbols
            String normalName = TESTDIR + rand8char() + ".tmp";
            String crazyName = TESTDIR + oneByteCharacters + twoByteCharacters + fourByteCharacters;
            byte[] content = Encoding.UTF8.GetBytes( "This is a really crazy name." );

            // normal name
            this.esu.CreateObjectOnPath( new ObjectPath( normalName ), null, null, content, "text/plain" );

            // crazy multi-byte character name
            this.esu.Rename( new ObjectPath( normalName ), new ObjectPath( crazyName ), true );

            // Wait for overwrite to complete
            System.Threading.Thread.Sleep( 5000 );

            // verify name in directory list
            bool found = false;
            foreach ( DirectoryEntry entry in this.esu.ListDirectory( new ObjectPath( TESTDIR ), null ) ) {
                if ( entry.Path.ToString().Equals( crazyName ) ) {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue( found, "crazyName not found in directory listing" );

            // Read back the content
            Assert.IsTrue( content.SequenceEqual( this.esu.ReadObject( new ObjectPath( crazyName ), null, null ) ), "object content wrong" );
        }

        /// <summary>
        /// Tests fetching data with multiple ranges.
        /// </summary>
        [TestMethod()]
        public void testMultipleRanges() {
            string input = "Four score and seven years ago";
            ObjectId id = this.esu.CreateObject(null, null, Encoding.UTF8.GetBytes(input), "text/plain" );
            cleanup.Add( id );
            Assert.IsNotNull( id, "Object null" );

            Extent[] extents = new Extent[5];
            extents[0] = new Extent( 27, 2 ); //ag
            extents[1] = new Extent( 9, 1 ); // e
            extents[2] = new Extent( 5, 1 ); // s
            extents[3] = new Extent( 4, 1 ); // ' '
            extents[4] = new Extent( 27, 3 ); // ago

            MultipartEntity entity = this.esu.ReadObjectExtents( id, extents );
            string content = Encoding.UTF8.GetString( entity.AggregateBytes() );
            Assert.AreEqual("ages ago", content, "Content incorrect");
        }

        /// <summary>
        /// x-emc-expiration-period only works on ECS Atmos
        /// </summary>
        [TestMethod()]
        public void testECSExpirationPeriod()
        {
            MetadataList mlist = new MetadataList();
            mlist.AddMetadata(new Metadata("x-emc-expiration-period", "5", false));
            ObjectId id = this.esu.CreateObject(null, mlist, Encoding.UTF8.GetBytes("Test expiration-period"), "text/plain");
            Assert.IsNotNull(id, "null ID returned");

            ObjectInfo info = this.esu.GetObjectInfo(id);
            Assert.IsNotNull(info, "ObjectInfo null");
            Assert.IsTrue(info.Expiration.Enabled);

            // Wait for exiration
            System.Threading.Thread.Sleep(5000);
            try
            {
                info = this.esu.GetObjectInfo(id);
                Assert.Fail("Exception not thrown! Object should not exist after exipiration.");
            }
            catch (EsuException e)
            {
                // This should happen.
                Assert.AreEqual(1003, e.Code, "Expected 1003 for not found");
            }
        }

        /// <summary>
        /// x-emc-retention-period only works on ECS Atmos
        /// </summary>
        [TestMethod()]
        public void testECSRetentionPeriod()
        {
            MetadataList mlist = new MetadataList();
            mlist.AddMetadata(new Metadata("x-emc-retention-period", "5", false));
            ObjectId id = this.esu.CreateObject(null, mlist, Encoding.UTF8.GetBytes("Test expiration-period"), "text/plain");
            Assert.IsNotNull(id, "null ID returned");

            ObjectInfo info = this.esu.GetObjectInfo(id);
            Assert.IsNotNull(info, "ObjectInfo null");
            Assert.IsTrue(info.Retention.Enabled);

            try
            {
                this.esu.DeleteObject(id);
                Assert.Fail("Exception not thrown! Retention Object shouldn't get deleted.");
            }
            catch (EsuException e)
            {
                Assert.AreEqual(2003, e.Code, "Expected 2003 for deleting/modifying object under retention.");
            }

            // Wait for retention period to expire.
            System.Threading.Thread.Sleep(5000);
            this.esu.DeleteObject(id);
        }

        private void AssertTokenPolicy(AccessTokenType token, PolicyType policy)
        {
            if (token.ContentLengthRange != null)
            {
                Assert.AreEqual(policy.ContentLengthRange.From, token.ContentLengthRange.From, "policy differs");
                Assert.AreEqual(policy.ContentLengthRange.To, token.ContentLengthRange.To, "policy differs");
            }
            Assert.AreEqual(policy.Expiration, token.Expiration, "policy differs");
            Assert.AreEqual(policy.MaxDownloads, token.MaxDownloads, "policy differs");
            Assert.AreEqual(policy.MaxUploads, token.MaxUploads, "policy differs");
            if (policy.Source != null)
            {
                Assert.IsTrue(policy.Source.Allow.SequenceEqual(token.Source.Allow), "policy differs");
                Assert.IsTrue(policy.Source.Disallow.SequenceEqual(token.Source.Disallow), "policy differs");
            }
            if (token.FormField != null)
            {
                for (int i = 0; i < token.FormField.Length; i++)
                {
                    Assert.AreEqual(policy.FormField[i].Name, token.FormField[i].Name, "policy differs");
                    Assert.AreEqual(policy.FormField[i].Optional, token.FormField[i].Optional, "policy differs");
                }
            }
        }
    }
}
