using MessagesAPI.Manager.Properties;
using MessagesAPI.Models;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace MessagesAPI.Manager
{
    public class PostResult
    {
        /// <summary>
        /// Indicates whether or not the post was successful
        /// </summary>
        public bool IsPosted { get; set; }

        /// <summary>
        /// Custom or system message returned from posting 
        /// </summary>
        public string Message { get; set; }
    }

    public class BizIntegrationManager
    {
        public BizIntegrationManager()
        {
            PostResult = new PostResult { IsPosted = false, Message = string.Empty };
        }
        public PostResult PostResult { get; set; }

        public bool PostToBiz(string fileContent, string fileName, string contentType = "text/xml", byte[] bytes = null, string from = "", string to = "")
        {
            string protocol = Settings.Default.Protocol;
            string uri = Settings.Default.Uri;
            string port = Settings.Default.Port;
            string party = Settings.Default.Party;

            Console.Write("Sending to Biz ... ");
            string uriBiz =
                string.Format("{0}://{1}:{2}/msgsrv/http?from={3}&filename={4}&to={5}",
                    protocol, uri, port, from == "" ? party : from, fileName, to);
        
            HttpWebRequest webRequest = WebRequest.Create(new Uri(uriBiz)) as HttpWebRequest;
            webRequest.Proxy = WebRequest.DefaultWebProxy;
            webRequest.Credentials = CredentialCache.DefaultCredentials;
            webRequest.Proxy.Credentials = CredentialCache.DefaultCredentials;

            webRequest.ContentType = contentType;
            webRequest.Method = "POST";

            if (bytes == null)
            {
                bytes = Encoding.ASCII.GetBytes(fileContent);
            }
            webRequest.ContentLength = bytes.Length;

            try
            {
                Stream stream = webRequest.GetRequestStream();
                stream.Write(bytes, 0, bytes.Length);
                stream.Close();
                HttpWebResponse webResponse = webRequest.GetResponse() as HttpWebResponse;
                if (webResponse == null)
                {
                    PostResult.Message = "Web response return a null. Please try again or contact support";
                    PostResult.IsPosted = false;
                    return false;
                }

                using (var streamReader = new StreamReader(webResponse.GetResponseStream()))
                {
                    PostResult.Message = streamReader.ReadToEnd().Trim();
                    PostResult.IsPosted = true;
                    return true;
                }
            }
            catch (Exception exception)
            {
                PostResult.Message = string.Format("Exception occured sending to Biz. {0}. {1}. {2}", fileName, exception.Message, exception.InnerException);
                PostResult.IsPosted = false;

                return false;
            }
        }

        public bool SendPdf(string base64String, XElement order, Code code, Supplier supplier)
        {
            var orderNumber = order.Element("orderIdentification")?.Element("entityIdentification")?.Value
                ?? string.Empty; // "[Order Number Missing]";
            var shipToGln = order.Element("orderLineItem")?.Element("orderLineItemDetail")?.
                            Element("orderLogisticalInformation")?.Element("shipTo")?.Element("gln")?.Value
                ?? string.Empty; // "[Ship To Gln Missing]";
            var orderTypeCode = order.Element("orderTypeCode")?.Value == string.Empty ?
                 order.Element("orderTypeCode")?.Attribute("codeListVersion")?.Value :
                 order.Element("orderTypeCode")?.Value;

            //if (orderTypeCode == "")
            //{
            //    orderTypeCode = "[Order Type Missing]";
            //}

            byte[] bytes = Convert.FromBase64String(base64String);

            return false;
        }
    }
}
