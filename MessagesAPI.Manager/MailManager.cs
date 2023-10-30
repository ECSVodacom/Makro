using MessagesAPI.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Xml.Linq;

namespace MessagesAPI.Manager
{
    public static class MailManager
    {

        public static void SendEmail(string base64String, XElement order, Code code, Supplier supplier)
        {
            var orderNumber = order.Element("orderIdentification")?.Element("entityIdentification")?.Value ?? "[Order Number Missing]";
            var shipTo = order.Element("orderLineItem")?.Element("orderLineItemDetail")?.
                            Element("orderLogisticalInformation")?.Element("shipTo")?.
                            Element("additionalPartyIdentification")?.Value ?? "[Ship To Missing]";
            var shipToGln = order.Element("orderLineItem")?.Element("orderLineItemDetail")?.
                            Element("orderLogisticalInformation")?.Element("shipTo")?.Element("gln")?.Value ?? "[Ship To Gln Missing]";
            var orderType = order.Element("orderTypeCode").Value == string.Empty ?
                order.Element("orderTypeCode")?.Attribute("codeListVersion")?.Value :
                order.Element("orderTypeCode")?.Value;

            if (orderType == "")
            {
                orderType = "[Order Type Missing]";
            }

            byte[] bytes = Convert.FromBase64String(base64String);
            var stream = new MemoryStream(bytes);

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("petrus.daffue@vodacom.co.za", "Petrus Daffue");
            if (supplier.SendEmailAddress.Length > 0)
            {
                foreach (var address in supplier.SendEmailAddress.Split(';'))
                {
                    mail.To.Add(new MailAddress(address));
                }
                mail.Subject = $"{code.Name} - {shipTo} - {orderNumber} - {orderType}";
                mail.Attachments.Add(new Attachment(stream, $"{orderNumber}.pdf", "application/pdf"));
                SmtpClient client = new SmtpClient();
                client.Host = "email-smtp.us-east-1.amazonaws.com";
                client.Credentials = new NetworkCredential { UserName = "AKIA3RB3NEH5FE5D4Z3Y", Password = "BLRL7KsQeNAqPZkOAZADpaoZU11KGaaHc1Q2V3AQAgV1", };
                client.EnableSsl = true;
                client.Port = 25;

                client.Send(mail);
            }
        }
    }
}
