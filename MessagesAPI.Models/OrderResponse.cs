using System;
using System.Xml.Linq;

namespace MessagesAPI.Models
{
    public class OrderResponse
    {
        public DateTime EditDate { get; set; }
        public string Identifier { get; set; }
        public XDocument ResponseXmlFile { get; set; }
        public long StandardDocumentId { get; set; }
        public int StatusId { get; set; }
        public string XmlFile { get; set; }
        public string VendorCode { get; set; }
        public string SellerGln { get; set; }
        public string OrderType { get; set; }
        public string ShipToGln { get; set; }
    }
}
