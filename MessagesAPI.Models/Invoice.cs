namespace MessagesAPI.Models
{
    public class Invoice
    {
        public string OrderNumber { get; set; }
        public string SellerGln { get; set; }
        public string ReplacementSellerGln { get; set; }
    }
}
