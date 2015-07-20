using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EsuApiLib.Rest;
using EsuApiLib;
using System.Diagnostics;

namespace EsuApiTest
{
    /// <summary>
    /// Tests the CreateSubtenant method in ECS.
    /// </summary>
    [TestClass]
    public class CreateSubtenantTest
    {
        /// <summary>
        /// UID to run tests with.  Change this value to your UID.
        /// </summary>
        private String uid = "";
        /// <summary>
        /// Shared secret for UID.  Change this value to your UID's shared secret
        /// </summary>
        private String secret = "";
        /// <summary>
        /// Hostname or IP of ECS server.  Change this value to your server's
        /// hostname or ip address.
        /// </summary>
        private String host = "";

        /// <summary>
        /// Port of ESU server (usually 9022 or 9023)
        /// </summary>
        private int port = 9022;

        private EsuApi esu;

        [TestInitialize()]
        public void Setup()
        {
            esu = new EsuRestApi(host, port, uid, secret);

            // Disable "Expect: 100-continue" behavior
            ((EsuRestApi)esu).Set100Continue(false);
        }
        [TestMethod]
        public void TestCreateSubtenant()
        {
            string subID = esu.CreateSubtenant();
            Assert.IsNotNull(subID);
            Console.WriteLine("Created Subtenant ID " + subID);

            esu.DeleteSubtenant(subID);
        }
    }
}
