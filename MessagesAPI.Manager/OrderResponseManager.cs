using MessagesAPI.Gateway;
using MessagesAPI.Models;
using System.Linq;
using System.ServiceModel;
using System.Xml;
using System.Xml.Linq;

namespace MessagesAPI.Manager
{
    public class OrderResponseManager
    {
        public string ToM2North(string envelope)
        {
            bool isOrderResponse = false;

            XDocument xDocument = XDocument.Parse(envelope);
            var orderResponse = from or in xDocument.Descendants()
                                where or.Name.LocalName == "orderResponse"
                                select or;

            string receiverGln = string.Empty;
            PostData postData = null;

            XNamespace ns3 = "http://www.unece.org/cefact/namespaces/StandardBusinessDocumentHeader";
            var sbdh = from d in xDocument.Descendants()
                       where d.Name.LocalName == "StandardBusinessDocumentHeader"
                       select d;

            receiverGln = sbdh.First().Element(ns3 + "Receiver").Element(ns3 + "Identifier").Value;

            var gateway = new MessagesGateway();

            foreach (XElement xElement in orderResponse)
            {
                string orderNumber = xElement.Element("orderResponseIdentification").Element("entityIdentification").Value;
                string sellerGln = xElement.Element("seller").Element("gln").Value;

                postData = gateway.GetPostData(orderNumber, sellerGln, receiverGln);

                if (postData == null)
                {
                    throw new FaultException("API look-up failed. Verify API settings.", new FaultCode("Exception"));
                }

                if (!string.IsNullOrEmpty(postData?.SellerGln))
                {
                    xElement.Element("seller").Element("gln").SetValue(postData.SellerGln);
                }

                isOrderResponse = true;
            }

            XmlDocument xml = TransportManager.XDocumentToXmlDocument(XDocument.Parse(xDocument.ToString()));

            if (postData == null)
            {
                return TransportManager.SendRequest(xml, throwFaultException: isOrderResponse);
            }

            return TransportManager.SendRequest(xml, postData?.UserName, postData?.Password, postData?.EndPoint, isOrderResponse);
        }
    }
}
