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
using System.IO;
using System.Linq;
using System.Text;
using EsuApiLib;
using EsuApiLib.Multipart;

namespace EsuApiTest
{
    [TestClass()]
    public class MultipartEntityTest
    {
        private const string BOUNDARY = "--bound0508812b8a8ad7";

        public MultipartEntityTest() { }

        [TestMethod()]
        public void testEmptyStream() {
            try {
                MultipartEntity.FromStream( new MemoryStream( new byte[]{} ), BOUNDARY );
                Assert.Fail( "empty stream should throw a parse exception" );
            } catch ( MultipartException ) {
                // expected
            }
        }

        [TestMethod()]
        public void testOnePart() {
            String eol = "\n";
            String partString = eol +
                                BOUNDARY + eol +
                                "Content-Type: text/plain" + eol +
                                "Content-Range: bytes 27-28/30" + eol +
                                eol +
                                "ag" + eol +
                                BOUNDARY + "--" + eol;
            MultipartEntity entity = MultipartEntity.FromStream( new MemoryStream( Encoding.UTF8.GetBytes( partString ) ),
                                                                 BOUNDARY );
            Assert.AreEqual( entity.Count(), 1, "Wrong number of parts" );
            Assert.AreEqual( entity[0].ContentType, "text/plain", "Part 1 content type is wrong" );
            Assert.AreEqual( entity[0].ContentExtent, new Extent( 27, 2 ), "Part 1 range is wrong" );
            Assert.IsTrue( entity[0].Data.SequenceEqual( Encoding.UTF8.GetBytes( "ag" ) ), "Part 1 data is wrong" );
        }

        [TestMethod()]
        public void testPartsNoCR() {
            String eol = "\n";
            String partString = eol +
                                BOUNDARY + eol +
                                "Content-Type: text/plain" + eol +
                                "Content-Range: bytes 27-28/30" + eol +
                                eol +
                                "ag" + eol +
                                BOUNDARY + eol +
                                "Content-Type: text/plain" + eol +
                                "Content-Range: bytes 9-9/30" + eol +
                                eol +
                                "e" + eol +
                                BOUNDARY + eol +
                                "Content-Type: text/plain" + eol +
                                "Content-Range: bytes 5-5/30" + eol +
                                eol +
                                "s" + eol +
                                BOUNDARY + eol +
                                "Content-Type: text/plain" + eol +
                                "Content-Range: bytes 4-4/30" + eol +
                                eol +
                                " " + eol +
                                BOUNDARY + eol +
                                "Content-Type: text/plain" + eol +
                                "Content-Range: bytes 27-29/30" + eol +
                                eol +
                                "ago" + eol +
                                BOUNDARY + "--" + eol;
            MultipartEntity entity = MultipartEntity.FromStream( new MemoryStream( Encoding.UTF8.GetBytes( partString ) ),
                                                                 BOUNDARY );
            Assert.AreEqual( entity.Count(), 5, "Wrong number of parts" );
            Assert.AreEqual( entity[0].ContentType, "text/plain", "Part 1 content type is wrong" );
            Assert.AreEqual( entity[0].ContentExtent, new Extent( 27, 2 ), "Part 1 range is wrong" );
            Assert.IsTrue( entity[0].Data.SequenceEqual( Encoding.UTF8.GetBytes( "ag" ) ), "Part 1 data is wrong" );
            Assert.AreEqual( entity[1].ContentType, "text/plain", "Part 2 content type is wrong" );
            Assert.AreEqual( entity[1].ContentExtent, new Extent( 9, 1 ), "Part 2 range is wrong" );
            Assert.IsTrue(entity[1].Data.SequenceEqual(Encoding.UTF8.GetBytes("e")), "Part 2 data is wrong");
            Assert.AreEqual( entity[2].ContentType, "text/plain", "Part 3 content type is wrong" );
            Assert.AreEqual( entity[2].ContentExtent, new Extent( 5, 1 ), "Part 3 range is wrong" );
            Assert.IsTrue(entity[2].Data.SequenceEqual(Encoding.UTF8.GetBytes("s")), "Part 3 data is wrong");
            Assert.AreEqual( entity[3].ContentType, "text/plain", "Part 4 content type is wrong" );
            Assert.AreEqual( entity[3].ContentExtent, new Extent( 4, 1 ), "Part 4 range is wrong" );
            Assert.IsTrue(entity[3].Data.SequenceEqual(Encoding.UTF8.GetBytes(" ")), "Part 4 data is wrong");
            Assert.AreEqual( entity[4].ContentType, "text/plain", "Part 5 content type is wrong" );
            Assert.AreEqual( entity[4].ContentExtent, new Extent( 27, 3 ), "Part 5 range is wrong" );
            Assert.IsTrue(entity[4].Data.SequenceEqual(Encoding.UTF8.GetBytes("ago")), "Part 5 data is wrong");
        }

        [TestMethod()]
        public void testPartsCR() {
            String eol = "\r\n";
            String partString = eol +
                                BOUNDARY + eol +
                                "Content-Type: text/plain" + eol +
                                "Content-Range: bytes 27-28/30" + eol +
                                eol +
                                "ag" + eol +
                                BOUNDARY + eol +
                                "Content-Type: text/plain" + eol +
                                "Content-Range: bytes 9-9/30" + eol +
                                eol +
                                "e" + eol +
                                BOUNDARY + eol +
                                "Content-Type: text/plain" + eol +
                                "Content-Range: bytes 5-5/30" + eol +
                                eol +
                                "s" + eol +
                                BOUNDARY + eol +
                                "Content-Type: text/plain" + eol +
                                "Content-Range: bytes 4-4/30" + eol +
                                eol +
                                " " + eol +
                                BOUNDARY + eol +
                                "Content-Type: text/plain" + eol +
                                "Content-Range: bytes 27-29/30" + eol +
                                eol +
                                "ago" + eol +
                                BOUNDARY + "--" + eol;
            MultipartEntity entity = MultipartEntity.FromStream( new MemoryStream( Encoding.UTF8.GetBytes( partString ) ),
                                                                 BOUNDARY );
            Assert.AreEqual( entity.Count(), 5, "Wrong number of parts" );
            Assert.AreEqual( entity[0].ContentType, "text/plain", "Part 1 content type is wrong" );
            Assert.AreEqual(entity[0].ContentExtent, new Extent(27, 2), "Part 1 range is wrong");
            Assert.IsTrue(entity[0].Data.SequenceEqual(Encoding.UTF8.GetBytes("ag")), "Part 1 data is wrong");
            Assert.AreEqual( entity[1].ContentType, "text/plain", "Part 2 content type is wrong" );
            Assert.AreEqual(entity[1].ContentExtent, new Extent(9, 1), "Part 2 range is wrong");
            Assert.IsTrue(entity[1].Data.SequenceEqual(Encoding.UTF8.GetBytes("e")), "Part 2 data is wrong");
            Assert.AreEqual( entity[2].ContentType, "text/plain", "Part 3 content type is wrong" );
            Assert.AreEqual(entity[2].ContentExtent, new Extent(5, 1), "Part 3 range is wrong");
            Assert.IsTrue(entity[2].Data.SequenceEqual(Encoding.UTF8.GetBytes("s")), "Part 3 data is wrong");
            Assert.AreEqual( entity[3].ContentType, "text/plain", "Part 4 content type is wrong" );
            Assert.AreEqual(entity[3].ContentExtent, new Extent(4, 1), "Part 4 range is wrong");
            Assert.IsTrue(entity[3].Data.SequenceEqual(Encoding.UTF8.GetBytes(" ")), "Part 4 data is wrong");
            Assert.AreEqual( entity[4].ContentType, "text/plain", "Part 5 content type is wrong" );
            Assert.AreEqual(entity[4].ContentExtent, new Extent(27, 3), "Part 5 range is wrong");
            Assert.IsTrue(entity[4].Data.SequenceEqual(Encoding.UTF8.GetBytes("ago")), "Part 5 data is wrong");
        }

        [TestMethod()]
        public void testCorruptedBoundary() {
            String eol = "\r\n";
            String partString = eol +
                                BOUNDARY + eol +
                                "Content-Type: text/plain" + eol +
                                "Content-Range: bytes 27-28/30" + eol +
                                eol +
                                "ago" + eol +
                                BOUNDARY + "- -" + eol;
            try {
                MultipartEntity.FromStream( new MemoryStream( Encoding.UTF8.GetBytes( partString ) ), BOUNDARY );
                Assert.Fail( "corrupted boundary should throw a parse exception" );
            } catch ( MultipartException ) {
                // expected
            }
        }

        [TestMethod()]
        public void testCorruptedByteRange() {
            String eol = "\r\n";
            String partString = eol +
                                BOUNDARY + eol +
                                "Content-Type: text/plain" + eol +
                                "Content-Range: bytes 27-28/30" + eol +
                                eol +
                                "ag" + eol +
                                BOUNDARY + eol +
                                "Content-Type: text/plain" + eol +
                                "Content-Range: bytes 9-9/30" + eol +
                                eol +
                                "e" + eol +
                                BOUNDARY + eol +
                                "Content-Type: text/plain" + eol +
                                "Content-Range: bytes 27-32/30" + eol +
                                eol +
                                "ago" + eol +
                                BOUNDARY + "--" + eol;
            try {
                MultipartEntity.FromStream( new MemoryStream( Encoding.UTF8.GetBytes( partString ) ), BOUNDARY );
                Assert.Fail( "corrupted byte range should throw a parse exception" );
            } catch ( MultipartException ) {
                // expected
            }
        }
    }
}
