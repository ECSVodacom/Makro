namespace MessagesAPI.Models
{
    public class StandardBusinessDocumentHeader
    {
        public Sender Sender { get; set; }
        public Receiver Receiver { get; set; }
        public DocumentIdentification DocumentIdentification { get; set; }
        public Manifest Manifest { get; set; }
    }
}
