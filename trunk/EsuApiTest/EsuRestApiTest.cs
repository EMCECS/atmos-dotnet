// Copyright � 2008, EMC Corporation.
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

namespace EsuApiLib.Rest {
    /// <summary>
    /// Tests the REST version of the ESU api.
    /// </summary>
    [TestClass()]
    public class EsuRestApiTest : EsuApiTest {
        /// <summary>
        /// UID to run tests with.  Change this value to your UID.
        /// </summary>
        private String uid = "<Full Token ID>";
        /// <summary>
        /// Shared secret for UID.  Change this value to your UID's shared secret
        /// </summary>
        private String secret = "<Secret Key>";
        /// <summary>
        /// Hostname or IP of ESU server.  Change this value to your server's
        /// hostname or ip address.
        /// </summary>
        private String host = "<Atmos IP>";

        /// <summary>
        /// Port of ESU server (usually 80 or 443)
        /// </summary>
        private int port = 80;

        [TestInitialize()]
        public void Setup()
        {
            base.SetUp();
            esu = new EsuRestApi(host, port, uid, secret);

            // Disable "Expect: 100-continue" behavior
            ((EsuRestApi)esu).Set100Continue(false);
        }

        /// <summary>
        /// Creates a new test object.
        /// </summary>
        public EsuRestApiTest() {
        }

        /// <summary>
        /// Test handling signature failures.  Should throw an exception with
        /// error code 1032.
        /// </summary>
        [TestMethod()]
        public void testSignatureFailure() {
            // break the secret key
            string badSecret = this.secret.ToUpper();
            this.esu = new EsuRestApi( host, port, uid, badSecret );

            try {
                // Create an object.  Should fail.
                ObjectId id = this.esu.CreateObject( null, null, null, null );
            } catch ( EsuException e ) {
                Assert.AreEqual( 1032, e.Code,
                "Expected error code 1032 for signature failure" );
                return;
            }
            Assert.Fail( "Exception not thrown!" );
        }

        /// <summary>
        /// Test general HTTP errors by generating a 404.
        /// </summary>
        [TestMethod()]
        public void testFourOhFour() {
            // break the context root
            ((EsuRestApi)this.esu).Context = "/restttt";
            try {
                ObjectId id = this.esu.CreateObject( null, null, null, null );
            } catch ( EsuException e ) {
                Assert.AreEqual( 404, e.Code,
                "Expected error code 404 for not found" );
                return;
            }
            Assert.Fail( "Exception not thrown!" );

        }

        [TestMethod()]
        public void testServerOffset()
        {
            // Check the server offset
            ((EsuRestApi)this.esu).ServerOffset = ((EsuRestApi)this.esu).CalculateServerOffset();
            Console.WriteLine("Server offset is " + ((EsuRestApi)this.esu).ServerOffset + " seconds");

            // Try creating an object
            testCreateEmptyObject();

            // Try a nonzero offset in case the clocks are in sync
            ((EsuRestApi)this.esu).ServerOffset = 15;
            testCreateEmptyObject();

        }

        /// <summary>
        /// NOTE: This doesn't actually test if the headers are present in the request.
        ///       Use tcpmon or wireshark to verify.
        /// </summary>
        [TestMethod()]
        public void testCustomHeaders()
        {
            Dictionary<string, string> customHeaders = new Dictionary<string, string>();
            customHeaders.Add("myCustomHeader", "Hello World!");
            ((EsuRestApi) this.esu).CustomHeaders = customHeaders;
            this.esu.GetServiceInformation();
        }
    }
}
