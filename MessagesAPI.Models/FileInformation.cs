namespace MessagesAPI.Models
{
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public class FileInformation
    {
        public string FileSize { get; set; }
        public string FileName { get; set; }
        public string FileStore { get; set; }
        public string FileType { get; set; }
    }
}