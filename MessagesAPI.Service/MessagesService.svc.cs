using MessagesAPI.Manager;
using System;
using System.Linq;
using System.ServiceModel;
using System.Xml.Linq;

namespace MessagesAPI.Service
{
    public class MessagesService : IMessagesService
    {
        [FaultContract(typeof(MessagesService))]
        public string ToM2North(string envelope)
        {
            XDocument xDocument = XDocument.Parse(envelope);
            var orderResponse = from or in xDocument.Descendants()
                                where or.Name.LocalName == "orderResponse"
                                select or;

            if (orderResponse.FirstOrDefault() != null)
            {
                var orderResponseManager = new OrderResponseManager();
                return orderResponseManager.ToM2North(envelope);
            }

            var invoiceManager = new InvoiceManager();
            return invoiceManager.ToM2North(envelope);
        }
    }
}
