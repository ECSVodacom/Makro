using MessagesAPI.Gateway;
using MessagesAPI.Models;
using System.Linq;
using System.ServiceModel;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.Linq;

namespace MessagesAPI.Manager
{
    public class InvoiceManager
    {
        public string ToM2North(string envelope)
        {
            XDocument xDocument = XDocument.Parse(envelope);

            var invoiceResponse = from or in xDocument.Descendants()
                                  where or.Name.LocalName == "invoice"
                                  select or;

            string receiverGln = string.Empty;
            PostData postData = null;

            XNamespace ns3 = "http://www.unece.org/cefact/namespaces/StandardBusinessDocumentHeader";
            var sbdh = (from d in xDocument.Descendants()
                        where d.Name.LocalName == "StandardBusinessDocumentHeader"
                        select d).First();

            // Use the receiver GLN  and find the Username and password from the Suppliers Table
            // The supplier table then links up to the API table which we will use to find the endpoint. 


            receiverGln = sbdh.Element(ns3 + "Receiver").Element(ns3 + "Identifier").Value;

            var gateway = new MessagesGateway();

            XElement xElement = invoiceResponse.ElementAt(0);

            string orderNumber = xElement.Element("purchaseOrder").Element("entityIdentification").Value;
            string sellerGln = xElement.Element("seller").Element("gln").Value;

            postData = gateway.GetPostData(orderNumber, sellerGln, receiverGln);

            if (postData == null)
            {
                throw new FaultException($"API look-up failed. Verify API settings and that order {orderNumber} for API gln {receiverGln}", new FaultCode("Exception"));
            }

            if (!string.IsNullOrEmpty(postData?.SellerGln))
            {
                xElement.Element("seller").Element("gln").SetValue(postData.SellerGln);
            }


            XmlDocument xml = TransportManager.XDocumentToXmlDocument(XDocument.Parse(xDocument.ToString()));

            if (postData == null)
            {
                return TransportManager.SendRequest(xml, throwFaultException: false);
            }

            return TransportManager.SendRequest(xml, postData?.UserName, postData?.Password, postData?.EndPoint, throwFaultException: false);
        }
    }
}
