using System.Collections.Generic;

namespace MessagesAPI.Models
{
    public class Supplier
    {
        public bool AcceptRevisedAllocationOrders { get; set; }
        public bool AcceptRevisedNormalOrders { get; set; }
        public bool CompleteOrderResponse { get; set; }
        public bool? UseAccountKey { get; set; }
        public bool? DoSendEmails { get; set; }
        public bool? SendPdf { get; set; }
        public IList<SupplierVendor> SupplierVendors { get; set; }
        public int Id { get; set; }
        public string AccountKey { get; set; }
        public string ApiEndPoint { get; set; }
        public string ApiGln { get; set; }
        public string ApiName { get; set; }
        public string ApiNameSpace { get; set; }
        public string Ean { get; set; }
        public string FileNamePrefix { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string SellerEan { get; set; }
        public string SendEmailAddress { get; set; }
        public string UserName { get; set; }
        public string NormalOrderCodes { get; set; }
        public string MultiStoreOrderCodes { get; set; }
        public string RejectOrderCodes { get; set; }

        public string SendPdfToBizOrderCodes { get; set; }
    }
}