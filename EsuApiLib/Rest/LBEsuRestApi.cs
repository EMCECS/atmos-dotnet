using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace EsuApiLib.Rest
{
    /// <summary>
    /// Software load balanced version of the EsuRestApi.  Distributes Atmos requests across
    /// multiple hosts using a simple round-robin algorithm.
    /// </summary>
    public class LBEsuRestApi : EsuRestApi
    {
        private string[] hosts;
        private int requests;

        /// <summary>
        /// Creates a new load-balanced EsuRestApi.
        /// </summary>
        /// <param name="hosts">The list of hosts to round-robin</param>
        /// <param name="port">The port, generally 80 for http and 443 for https</param>
        /// <param name="uid">The UID in the form of subtenantID/UID</param>
        /// <param name="secret">The UID's shared secret key.</param>
        public LBEsuRestApi(string[] hosts, int port, string uid, string secret) : base(hosts[0], port, uid, secret) {
            requests = 0;
            this.hosts = hosts;
        }

        /// <summary>
        /// Builds the URL to the resource.
        /// </summary>
        /// <param name="resource">The Atmos resource</param>
        /// <returns>a Uri object</returns>
        protected override Uri buildUrl(string resource)
        {
            int n = Interlocked.Increment(ref requests);
            string host = hosts[n % hosts.Length];
            return new Uri(protocol + "://" + host + ":" + port + resource);
        }
        
    }
}
