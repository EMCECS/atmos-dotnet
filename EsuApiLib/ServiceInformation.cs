using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EsuApiLib
{
    /// <summary>
    /// Used to return the response from the GetServiceInformation call.
    /// </summary>
    public class ServiceInformation
    {
        /// <summary>
        /// The version of Atmos in the form of major.minor.patch, e.g. 1.4.0
        /// </summary>
        public string AtmosVersion{set;get;}

    }
}
