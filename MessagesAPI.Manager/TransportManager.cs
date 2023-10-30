using MessagesAPI.Manager.Properties;
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MessagesAPI.Manager
{
    public class TransportManager
    {
        public static bool AllwaysGoodCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            return true;
        }

        public static XmlDocument XDocumentToXmlDocument(XDocument xDocument)
        {
            var xmlDocument = new XmlDocument();
            using (var xmlReader = xDocument.CreateReader())
            {
                xmlDocument.Load(xmlReader);
                return xmlDocument;
            }
        }

        public static string SendRequest(XmlDocument envelope, string username = "", string password = "", string url = "", bool throwFaultException = false)
        {
            if (string.IsNullOrEmpty(url))
            {
                url = Settings.Default.Endpoint;
            }

            HttpWebRequest webRequest = CreateWebRequest(url, username, password);
            webRequest.Timeout = 2147483647;
            InsertSoapEnvelopeIntoWebRequest(envelope, webRequest);

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                string webSvcCertificateThumbPrint = Settings.Default.WebSvcCertificateThumbPrint;
                var cert = CertificateManager.GetCertificateByThumbprint(webSvcCertificateThumbPrint, StoreLocation.LocalMachine);

                if (cert == null)
                    return string.Format("Certificate with thumb print {0} could not be found!", webSvcCertificateThumbPrint);

                ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(AllwaysGoodCertificate);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                webRequest.ClientCertificates.Add(cert);
            }

            string result = string.Empty;

            try
            {
                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                    {
                        result = rd.ReadToEnd();
                    }
                }
            }
            catch (WebException ex)
            {
                WebResponse errorRespone = ex.Response;
                using (Stream respStream = errorRespone.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(respStream);
                    string text = reader.ReadToEnd();
                    result = text;

                    XmlDocument responseXml = new XmlDocument();

                    if (IsValidXml(text))
                    {
                        responseXml.LoadXml(text);
                        XmlNodeList faulstrings = responseXml.GetElementsByTagName("faultstring");

                        result = faulstrings[0].InnerXml;
                    }

                    if (throwFaultException)
                    {
                        if (result != "Order not found."
                            && result != "Xdoc already responded to." &&
                            result != "Doc already responded to.")
                        {
                            throw new FaultException(result, new FaultCode("Exception"));
                        }
                    }
                }
            }



            return result;
        }

        public static bool IsValidXml(string xml)
        {
            try
            {
                XDocument.Parse(xml);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static HttpWebRequest CreateWebRequest(string url, string username, string password)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Proxy = WebRequest.DefaultWebProxy;
            webRequest.Credentials = CredentialCache.DefaultCredentials;
            webRequest.Proxy.Credentials = CredentialCache.DefaultCredentials;
            webRequest.Method = "POST";
            webRequest.ContentType = "text/xml";
            webRequest.Accept = "text/xml, text/html, image/gif, image/jpeg, *; q=.2, */*; q=.2";

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                string credentials = username + ":" + password;
                byte[] credentialsBytes = Encoding.UTF8.GetBytes(credentials);
                string authorization = "Basic " + Convert.ToBase64String(credentialsBytes);
                webRequest.Headers.Add("Authorization", authorization);
            }

            return webRequest;
        }

        private static void InsertSoapEnvelopeIntoWebRequest(XmlDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            using (Stream stream = webRequest.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }
        }
    }
}