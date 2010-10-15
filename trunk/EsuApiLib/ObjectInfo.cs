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
using System.Linq;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace EsuApiLib
{
    /// <summary>
    /// Encapsulates information about an object including its replicas,
    /// retention, and expiration.
    /// </summary>
    public class ObjectInfo
    {
        /// <summary>
        /// Raw XML returned from the GetObjectInfo call
        /// </summary>
        public string RawXml { set; get; }

        /// <summary>
        /// The ID of the object
        /// </summary>
        public ObjectId ObjectID { set; get; }

        /// <summary>
        /// The object selection
        /// </summary>
        public string Selection { set; get; }

        /// <summary>
        /// Object replicas (where the object is stored)
        /// </summary>
        public List<ObjectReplica> Replicas { set; get; }

        /// <summary>
        /// Retention policy for the object
        /// </summary>
        public ObjectRetention Retention { set; get; }

        /// <summary>
        /// Expiration policy for the object
        /// </summary>
        public ObjectExpiration Expiration { set; get; }

        /// <summary>
        /// Constructs a new ObjectInfo from the given XML
        /// </summary>
        /// <param name="xml">XML from the GetObjectInfo call</param>
        public ObjectInfo(string xml)
        {
            Replicas = new List<ObjectReplica>();
            this.RawXml = xml;
            parse(xml);
            Debug.WriteLine("Replica XML: " + xml);
        }

        /// <summary>
        /// Constructs an empty ObjectInfo
        /// </summary>
        public ObjectInfo()
        {
            Replicas = new List<ObjectReplica>();
        }

        /// <summary>
        /// Fills in the ObjectInfo from the XML
        /// </summary>
        /// <param name="xml"></param>
        private void parse(string xml)
        {
            XmlDocument d = new XmlDocument();
            d.LoadXml(xml);

            XmlElement root = (XmlElement)d.GetElementsByTagName("GetObjectInfoResponse")[0];

            foreach (XmlNode xn in root.ChildNodes)
            {
                if (!(xn is XmlElement))
                {
                    continue;
                }
                XmlElement xe = (XmlElement)xn;
                string tagName = xe.LocalName;
                if ("objectId".Equals(tagName))
                {
                    this.ObjectID = new ObjectId(xe.InnerText);
                }
                else if ("selection".Equals(tagName))
                {
                    this.Selection = xe.InnerText;
                }
                else if ("replicas".Equals(tagName))
                {
                    processReplicas(xe);
                }
                else if ("expiration".Equals(tagName))
                {
                    processExpiration(xe);
                }
                else if ("retention".Equals(tagName))
                {
                    processRetention(xe);
                }
                else if ("numReplicas".Equals(tagName))
                {
                    // Ignore
                }
                else
                {
                    throw new EsuException("Unknown ObjectInfo tag " + tagName);
                }
            }
        }

        /// <summary>
        /// Extracts the replicas from the element
        /// </summary>
        /// <param name="xe">replicas element</param>
        private void processReplicas(XmlElement xe)
        {
            foreach (XmlNode repchild in xe.ChildNodes)
            {
                if (!(repchild is XmlElement))
                {
                    continue;
                }
                XmlElement repelement = (XmlElement)repchild;
                string tagName = repelement.LocalName;
                if ("replica".Equals(tagName))
                {
                    processReplica(repelement);
                }
                else
                {
                    throw new EsuException("Unkown Replicas child " + tagName);
                }
            }
        }

        /// <summary>
        /// Parses a replica from the element
        /// </summary>
        /// <param name="xe">The replica element</param>
        private void processReplica(XmlElement xe)
        {
            foreach (XmlNode repnode in xe.ChildNodes)
            {
                if (!(repnode is XmlElement))
                {
                    continue;
                }
                XmlElement repelement = (XmlElement)repnode;
                string tagName = repelement.LocalName;
                ObjectReplica rep = new ObjectReplica();
                if ("id".Equals(tagName))
                {
                    rep.Id = repelement.InnerText;
                }
                else if ("type".Equals(tagName))
                {
                    rep.ReplicaType = repelement.InnerText;
                }
                else if ("current".Equals(tagName))
                {
                    rep.Current = repelement.InnerText.Equals("true");
                }
                else if ("location".Equals(tagName))
                {
                    rep.Location = repelement.InnerText;
                }
                else if ("storageType".Equals(tagName))
                {
                    rep.StorageType = repelement.InnerText;
                }
                else
                {
                    throw new EsuException("Unknown replica child: " + tagName);
                }

                this.Replicas.Add(rep);
            }
        }

        /// <summary>
        /// Parses the expiration information from the element
        /// </summary>
        /// <param name="xe">the expiration element</param>
        private void processExpiration(XmlElement xe)
        {
            ObjectExpiration oe = new ObjectExpiration();
            foreach (XmlNode node in xe.ChildNodes)
            {
                if (!(node is XmlElement))
                {
                    continue;
                }
                XmlElement ele = (XmlElement)node;
                string tagName = ele.LocalName;
                if ("enabled".Equals(tagName))
                {
                    oe.Enabled = ele.InnerText.Equals("true");
                }
                else if ("endAt".Equals(tagName))
                {
                    if (ele.InnerText != null && ele.InnerText.Length > 0)
                    {
                        oe.EndAt = parseDate(ele.InnerText);
                    }
                }
                else
                {
                    throw new EsuException("Unknown Expiration tag: " + tagName);
                }
            }
            this.Expiration = oe;
        }

        /// <summary>
        /// Parses retention information from the element
        /// </summary>
        /// <param name="xe">The retention element</param>
        private void processRetention(XmlElement xe)
        {
            ObjectRetention re = new ObjectRetention();
            foreach (XmlNode node in xe.ChildNodes)
            {
                if (!(node is XmlElement))
                {
                    continue;
                }
                XmlElement ele = (XmlElement)node;
                string tagName = ele.LocalName;
                if ("enabled".Equals(tagName))
                {
                    re.Enabled = ele.InnerText.Equals("true");
                }
                else if ("endAt".Equals(tagName))
                {
                    if (ele.InnerText != null && ele.InnerText.Length > 0)
                    {
                        re.EndAt = parseDate(ele.InnerText);
                    }
                }
                else
                {
                    throw new EsuException("Unknown Retention tag: " + tagName);
                }
            }
            this.Retention = re;
        }

        /// <summary>
        /// Parses a dateTime from a string
        /// </summary>
        /// <param name="text">the ISO8601 date string</param>
        /// <returns></returns>
        private DateTime parseDate(string text)
        {
            if (text == null || text.Trim().Length<1) 
            {
                // Since DateTime isn't an object in C# it 
                // can't be null.  Initialize it to min value.
                return DateTime.MinValue;
            }
            Debug.WriteLine("XML dateTime: " + text);
            return DateTime.Parse(text);
        }
    }
}
