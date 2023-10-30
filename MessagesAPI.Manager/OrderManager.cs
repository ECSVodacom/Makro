using MessagesAPI.Gateway;
using MessagesAPI.Models;
using MessagesAPI.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Supplier = MessagesAPI.Models.Supplier;

namespace MessagesAPI.Manager
{
    public class OrderManager
    {
        public XDocument BuildRespondOrder(StandardBusinessDocumentHeader dh, XElement order, Supplier supplier)
        {
            var orderNumber = order.Element("orderIdentification").Element("entityIdentification").Value;

            var updatedOrder = new XElement(order);
            updatedOrder.Element("orderIdentification").Name = "orderResponseIdentification";
            updatedOrder.Name = "orderResponse";
            updatedOrder.AddFirst(
                new XElement("responseStatusCode", new XAttribute("codeListVersion", ""), "received"),
                new XElement("orderResponseReasonCode")
            );

            XDocument respondOrder;
            if (supplier.UseAccountKey == true)
            {
                updatedOrder.Elements("orderLineItem").Remove();
                respondOrder = XDocument.Parse(string.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                xmlns:ws=""{10}""
                xmlns:stan=""http://www.unece.org/cefact/namespaces/StandardBusinessDocumentHeader"">
            <soapenv:Header><Auth><accountKey>{9}</accountKey></Auth></soapenv:Header>
               <soapenv:Body>
                  <ws:respondOrderRequest>
                    <ws:orderResponseMessageType>
                        <stan:StandardBusinessDocumentHeader>
                            <stan:Sender>
                                <stan:Identifier Authority=""{0}"">{1}</stan:Identifier>
                            </stan:Sender>
                            <stan:Receiver>
                                <stan:Identifier Authority=""{2}"">{3}</stan:Identifier>
                            </stan:Receiver>
                            <stan:DocumentIdentification>
                                <stan:Standard>{4}</stan:Standard>
                                <stan:InstanceIdentifier>{5}</stan:InstanceIdentifier>
                                <stan:Type>{6}</stan:Type>
                                <stan:CreationDateAndTime>{7}</stan:CreationDateAndTime>
                            </stan:DocumentIdentification>
                        </stan:StandardBusinessDocumentHeader>
                        {8}
                     </ws:orderResponseMessageType>
                  </ws:respondOrderRequest>
               </soapenv:Body>
            </soapenv:Envelope>",
                dh.Receiver.IdentifierAuthority, dh.Receiver.Value,
                dh.Sender.IdentifierAuthority, dh.Sender.Value,
                dh.DocumentIdentification.Standard,
                orderNumber,
                dh.DocumentIdentification.Type,
                dh.DocumentIdentification.CreationDateAndTime,
                updatedOrder,
                supplier.AccountKey,
                supplier.ApiNameSpace
            ));
            }
            else
            {
                foreach (var e in updatedOrder.Elements("orderLineItem"))
                {
                    e.Name = "orderResponseLineItem";
                }

                var shipTo = updatedOrder
                     .Elements("orderResponseLineItem")
                     .Elements("orderLineItemDetail")
                     .Elements("orderLogisticalInformation")
                     .Elements("shipTo").FirstOrDefault();

                updatedOrder.Element("seller").AddAfterSelf(new XElement(shipTo));

                respondOrder = XDocument.Parse(string.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                xmlns:ws=""{1}""
                xmlns:stan=""http://www.unece.org/cefact/namespaces/StandardBusinessDocumentHeader"">
            <soapenv:Header></soapenv:Header>
               <soapenv:Body>
                  <ws:respondOrderRequest>
                    <ws:orderResponseMessageType>
                        {0}
                     </ws:orderResponseMessageType>
                  </ws:respondOrderRequest>
               </soapenv:Body>
            </soapenv:Envelope>",
                updatedOrder,
                supplier.ApiNameSpace
            ));
            }

            return respondOrder;
        }

        /// <summary>
        /// Extract and build unique orders, send it to Biz
        /// </summary>
        /// <param name="original">Order response</param>
        /// <returns>Number of orders still awaiting collection</returns>
        public int ExtractOrders(XDocument original, Supplier supplier, long standardDocumentId, bool ignoreDuplicates = false)
        {
            XNamespace ns3 = "http://www.unece.org/cefact/namespaces/StandardBusinessDocumentHeader";
            XNamespace ns2 = supplier.ApiNameSpace;

            var numOrdersRemaining = Convert.ToInt32(original.Descendants(ns2 + "numOrdersRemaining").FirstOrDefault().Value);

            IEnumerable<XElement> orders = original.Descendants(ns2 + "orderMessageType").Descendants("order");

            if (orders.Count() == 0)
            {
                Console.WriteLine(string.Format("{0} does not have orders on the order request respond", supplier.Name));

                return 0;
            }

            var numberOfItems = original.Descendants(ns3 + "NumberOfItems").FirstOrDefault().Value;
            if (orders.Count() != Convert.ToInt32(numberOfItems))
            {
                Console.WriteLine(string.Format("{0} does not have all the orders on the order request respond. {1} orders in batch. {2} expected", supplier.Name, orders.Count(), numberOfItems));

                return 0;
            }

            foreach (XElement order in orders)
            {
                ProcessOrder(original, order, supplier, standardDocumentId, ignoreDuplicates);
            }



            return numOrdersRemaining;
        }

        public StandardBusinessDocumentHeader GetStandardBusinessDocumentHeader(XDocument xDocument)
        {
            StandardBusinessDocumentHeader standardBusinessDocumentHeader = null;

            XNamespace ns3 = "http://www.unece.org/cefact/namespaces/StandardBusinessDocumentHeader";
            var standardBusinessDocumentHeaderXml = from d in xDocument.Descendants()
                                                    where d.Name.LocalName == "StandardBusinessDocumentHeader"
                                                    select d;

            foreach (XElement xElement in standardBusinessDocumentHeaderXml)
            {
                standardBusinessDocumentHeader = new StandardBusinessDocumentHeader
                {
                    Sender = new Sender
                    {
                        IdentifierAuthority = xElement.Element(ns3 + "Sender").Element(ns3 + "Identifier").Attribute("Authority").Value.Replace("&", "&amp;"),
                        Value = xElement.Element(ns3 + "Sender").Element(ns3 + "Identifier").Value
                    },
                    Receiver = new Receiver
                    {
                        IdentifierAuthority = xElement.Element(ns3 + "Receiver").Element(ns3 + "Identifier").Attribute("Authority").Value.Replace("&", "&amp;"),
                        Value = xElement.Element(ns3 + "Receiver").Element(ns3 + "Identifier").Value
                    },
                    DocumentIdentification = new DocumentIdentification
                    {
                        Standard = xElement.Element(ns3 + "DocumentIdentification").Element(ns3 + "Standard").Value,
                        InstanceIdentifier = xElement.Element(ns3 + "DocumentIdentification").Element(ns3 + "InstanceIdentifier").Value,
                        Type = xElement.Element(ns3 + "DocumentIdentification").Element(ns3 + "Type").Value,
                        CreationDateAndTime = xElement.Element(ns3 + "DocumentIdentification").Element(ns3 + "CreationDateAndTime").Value,
                    },
                    Manifest = new Manifest
                    {
                        NumberOfItems = xElement.Element(ns3 + "Manifest").Element(ns3 + "NumberOfItems").Value,
                    }
                };
            }

            return standardBusinessDocumentHeader;
        }

        public bool IsValidXml(string xml)
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

        public void ProcessMessages()
        {
            MessagesGateway gateway = new MessagesGateway();
            Parallel.ForEach(gateway.GetSuppliers().AsEnumerable(), s =>
             {
                 var getOrders = true;
                 var supplier = s;

                 Console.WriteLine(string.Format("Getting orders for {1} {0}", s.Name, s.Id));
                 string ordersResponse = GetOrders(supplier);
                 XDocument xDocument = new XDocument();

                 if (IsValidXml(ordersResponse))
                 {
                     xDocument = XDocument.Parse(ordersResponse);
                 }
                 else
                 {
                     Console.WriteLine(string.Format("{3} {0} {1} {2}", s.Name, s.AccountKey, ordersResponse, s.Id));
                     getOrders = false;
                 }

                 while (getOrders)
                 {
                     var sbdh = GetStandardBusinessDocumentHeader(xDocument);
                     if (sbdh == null)
                     {
                         getOrders = false;
                         break;
                     }

                     var standardDocumentId = gateway.SaveStandardDocument(
                         sbdh.DocumentIdentification.InstanceIdentifier,
                         ordersResponse,
                         supplier.Id);

                     if (standardDocumentId == -1)
                         getOrders = false;
                     else
                         getOrders = ExtractOrders(xDocument, supplier, standardDocumentId) > 0 ? true : false;
                 }
             });
        }


        public void RePostToBiz()
        {
            MessagesGateway gateway = new MessagesGateway();

            gateway.StandardDocuments().ForEach(standardDocument =>
            {
                XDocument xDocument = XDocument.Parse(standardDocument.StandardDocumentXml);
                var supplier = gateway.GetSupplier(standardDocument.SupplierId);
                ExtractOrders(xDocument, supplier, standardDocument.Id, true);
                gateway.UpdateStandardDocument(standardDocument.Id);
            });
        }

        public string SendOrderResponse(XDocument makroRespondOrder, Supplier supplier)
        {
            XmlDocument makroRespondOrderXml = TransportManager.XDocumentToXmlDocument(makroRespondOrder);
            return TransportManager.SendRequest(makroRespondOrderXml, supplier.UserName, supplier.Password, supplier.ApiEndPoint);
        }

        private static XmlDocument GetOrderRequestWithCredentials(Supplier supplier)
        {
            XmlDocument ordersRequest = new XmlDocument();
            ordersRequest.LoadXml(string.Format(@"<soapenv:Envelope
                    xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                    xmlns:ws=""{0}"" >
                        <soapenv:Header/>
                        <soapenv:Body>
                            <ws:getOrdersRequest>
                                <ws:numOrders>20</ws:numOrders>
                            </ws:getOrdersRequest>
                        </soapenv:Body>
                    </soapenv:Envelope>", supplier.ApiNameSpace));

            return ordersRequest;
        }

        private static XmlDocument GetOrderRequestWithKey(Supplier supplier)
        {
            XmlDocument ordersRequest = new XmlDocument();
            ordersRequest.LoadXml(string.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
                <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ws=""{0}"">
                  <soapenv:Header>
                    <Auth>
                       <accountKey>{1}</accountKey>
                    </Auth>
                   </soapenv:Header>
                   <soapenv:Body>
                      <ws:getOrdersRequest>
                         <ws:numOrders>20</ws:numOrders>
                      </ws:getOrdersRequest>
                   </soapenv:Body>
                </soapenv:Envelope>", supplier.ApiNameSpace, supplier.AccountKey));

            return ordersRequest;
        }

        private static string GetOrders(Supplier supplier)
        {
            XmlDocument orderRequest;
            if (supplier.UseAccountKey == true)
            {
                orderRequest = GetOrderRequestWithKey(supplier);
            }
            else
            {
                orderRequest = GetOrderRequestWithCredentials(supplier);
            }

            return TransportManager.SendRequest(orderRequest, supplier.UserName, supplier.Password, supplier.ApiEndPoint);
        }

        //private static bool PostToBiz(string documentNumber, XDocument xDocument, string prefix = "MAKRO")
        //{
        //    if (string.IsNullOrEmpty(prefix))
        //    {
        //        prefix = "MAKRO";
        //    }
        //    string filename = $"{prefix}ORD-" + documentNumber + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xml";

        //    BizIntegrationManager bizIntegration = new BizIntegrationManager();
        //    bizIntegration.DoPost(xDocument.ToString(), filename);

        //    if (!bizIntegration.PostResult.IsPosted)
        //    {
        //        var gateway = new MessagesGateway();
        //        gateway.LogError(0, bizIntegration.PostResult.Message, 0);

        //    }

        //    return bizIntegration.PostResult.IsPosted;
        //}

        private void ProcessOrder(XDocument original, XElement order, Supplier supplier, long standardDocumentId, bool ignoreDuplicate = false)
        {
            var orderForResponse = new XElement(order);
            OrderResponse orderResponse;
            XDocument newlyBuilt = new XDocument(original);
            MessagesGateway gateway = new MessagesGateway();

            var standardBusinessDocumentHeader = GetStandardBusinessDocumentHeader(original);
            XNamespace ns3 = "http://www.unece.org/cefact/namespaces/StandardBusinessDocumentHeader";
            XNamespace ns2 = supplier.ApiNameSpace;

            if (supplier.UseAccountKey == false)
            {
                var buyerGln = order.Element("buyer")?.Element("gln")?.Value;
                if (buyerGln != null)
                {
                    newlyBuilt.Descendants(ns3 + "StandardBusinessDocumentHeader")
                           .Elements(ns3 + "Sender").FirstOrDefault().SetElementValue(ns3 + "Identifier", buyerGln);
                }
            }

            var orderNumber = order.Element("orderIdentification")?.Element("entityIdentification")?.Value ?? "[Order Number Missing]";
            var orderTypeCode = string.Empty;

            orderTypeCode = order.Element("orderTypeCode")?.Value == string.Empty ?
                order.Element("orderTypeCode")?.Attribute("codeListVersion")?.Value :
                order.Element("orderTypeCode")?.Value;

            if (orderTypeCode == "")
            {
                orderTypeCode = "[Order Type Missing]";
            }

            var sellerCodeToAcknowledge = order.Element("seller")?.Element("gln")?.Value;
            var sellerCode = order.Element("seller")?.Element("gln")?.Value?.TrimStart('0');
            var sellerDescription = from el in order.Elements("seller")?.Elements("additionalPartyIdentification")
                                    where (string)el.Attribute("additionalPartyIdentificationTypeCode") == "BUYER_ASSIGNED_ACCOUNTING_SUPPLIER_DESCRIPTION"
                                    select el.Value;
            var sellerCodeAdditional = from el in order.Elements("seller")?.Elements("additionalPartyIdentification")
                                       where (string)el.Attribute("additionalPartyIdentificationTypeCode") == "BUYER_ASSIGNED_ACCOUNTING_SUPPLIER_ID"
                                       select el.Value;

            var shipToGln = order.Element("orderLineItem")?.Element("orderLineItemDetail")?.
                           Element("orderLogisticalInformation")?.Element("shipTo")?.Element("gln")?.Value;


            if (sellerCodeAdditional != null)
            {
                sellerCode = sellerCodeAdditional.FirstOrDefault().TrimStart('0');
            }

            Code code = gateway.GetReceiverSellerGln(sellerCode, supplier.Id);
            if (code == null) //  && string.IsNullOrEmpty(supplier.Password)
            {
                gateway.SaveUnknownVendorCode(sellerCode, supplier);
                code = new Code
                {
                    ReceiverGln = supplier.Ean,
                    SellerGln = supplier.Ean,
                    Value = sellerCode,
                    Name = code?.Name ?? "Unknown"
                };
            }
            else if (code?.Name == null)
            {
                code.Name = sellerDescription.FirstOrDefault();
            };


            /* 
            * CN - Revised Normal Orders
            * CA - Revised Allocation Orders
            */
            if (orderTypeCode == "CN" || orderTypeCode == "CA")
            {
                var sendRevisedOrders = (orderTypeCode == "CN" && supplier.AcceptRevisedNormalOrders)
                    || (orderTypeCode == "CA" && supplier.AcceptRevisedAllocationOrders);

                if (sendRevisedOrders == false)
                {
                    orderResponse = new OrderResponse
                    {
                        Identifier = orderNumber,
                        XmlFile = order.ToString(),
                        ResponseXmlFile = newlyBuilt,
                        StatusId = 7,
                        StandardDocumentId = standardDocumentId,
                        EditDate = DateTime.Now,
                        SellerGln = sellerCodeToAcknowledge,
                        OrderType = orderTypeCode,
                        ShipToGln = shipToGln
                    };
                    ProcessOrderResponse(original, orderForResponse, orderResponse, supplier);

                    return;
                }
            }

            bool isNormalOrderCode = supplier.NormalOrderCodes.Contains(orderTypeCode);
            if (ignoreDuplicate == false)
            {
                //Order previousOrder = gateway.GetOrder(orderNumber, supplier);
                //if (previousOrder != null)
                if (gateway.IsDuplicate(orderNumber, orderTypeCode, shipToGln, supplier.Id, isNormalOrderCode))
                {
                    orderResponse = new OrderResponse
                    {
                        Identifier = orderNumber,
                        XmlFile = order.ToString(),
                        ResponseXmlFile = newlyBuilt,
                        StatusId = 5,
                        StandardDocumentId = standardDocumentId,
                        EditDate = DateTime.Now,
                        SellerGln = sellerCodeToAcknowledge,
                        OrderType = orderTypeCode,
                        ShipToGln = shipToGln
                    };
                    ProcessOrderResponse(original, orderForResponse, orderResponse, supplier);

                    return;
                }
            }

            if (code != null)
            {
                order.Element("seller").SetElementValue("gln", code.SellerGln);
                newlyBuilt.Descendants(ns3 + "StandardBusinessDocumentHeader")
                           .Elements(ns3 + "Receiver").FirstOrDefault().SetElementValue(ns3 + "Identifier", code.ReceiverGln);
            }

            newlyBuilt.Descendants(ns3 + "StandardBusinessDocumentHeader")
                                .Elements(ns3 + "DocumentIdentification").FirstOrDefault().SetElementValue(ns3 + "InstanceIdentifier", orderNumber);
            newlyBuilt.Descendants(ns3 + "StandardBusinessDocumentHeader")
                                .Elements(ns3 + "Manifest").FirstOrDefault().SetElementValue(ns3 + "NumberOfItems", "1");

            bool isSent = false;

            if (string.IsNullOrEmpty(orderTypeCode) == false && supplier.NormalOrderCodes.Contains(orderTypeCode)) /* Normal order */
            {
                newlyBuilt.Descendants(ns2 + "orderMessageType").Descendants("order").Remove();
                shipToGln = order.Element("orderLineItem").Element("orderLineItemDetail").
                          Element("orderLogisticalInformation").Element("shipTo").Element("gln").Value;
                newlyBuilt.Descendants(ns2 + "orderMessageType").FirstOrDefault().Add(order);

                //isSent = PostToBiz(orderNumber + "-" + shipToGln, newlyBuilt, supplier.FileNamePrefix);

                orderResponse = new OrderResponse
                {
                    Identifier = orderNumber,
                    XmlFile = order.ToString(),
                    ResponseXmlFile = newlyBuilt,
                    StatusId = isSent ? 1 : 6,
                    StandardDocumentId = standardDocumentId,
                    EditDate = DateTime.Now,
                    SellerGln = sellerCodeToAcknowledge,
                    OrderType = orderTypeCode,
                    ShipToGln = shipToGln
                };
                ProcessOrderResponse(original, orderForResponse, orderResponse, supplier);

            }
            else if (string.IsNullOrEmpty(orderTypeCode) == false && supplier.MultiStoreOrderCodes.Contains(orderTypeCode)) /* JAB order */
            {
                var alreadyRespondedTo = false; // Only send one order response per batch
                Dictionary<string, List<XElement>> dictionaryOrderLineItem = new Dictionary<string, List<XElement>>();
                foreach (var orderLineItem in order.Elements("orderLineItem"))
                {
                    shipToGln = orderLineItem.Element("orderLineItemDetail").Element("orderLogisticalInformation").Element("shipTo").Element("gln").Value;
                    if (dictionaryOrderLineItem.TryGetValue(shipToGln, out List<XElement> savedOrderLineItem))
                        savedOrderLineItem.Add(orderLineItem);
                    else
                    {
                        savedOrderLineItem = new List<XElement>
                        {
                            orderLineItem
                        };
                        dictionaryOrderLineItem.Add(shipToGln, savedOrderLineItem);
                    }
                }

                foreach (KeyValuePair<string, List<XElement>> keyValuePair in dictionaryOrderLineItem)
                {
                    newlyBuilt.Descendants(ns2 + "orderMessageType").Descendants("order").Remove();
                    order.Elements("orderLineItem").Remove();
                    order.Add(keyValuePair.Value);
                    newlyBuilt.Descendants(ns2 + "orderMessageType").FirstOrDefault().Add(order);

                    //isSent = PostToBiz(orderNumber + "-" + shipToGln, newlyBuilt, supplier.FileNamePrefix);

                    orderResponse = new OrderResponse
                    {
                        Identifier = orderNumber,
                        XmlFile = order.ToString(),
                        ResponseXmlFile = newlyBuilt,
                        StatusId = isSent ? 1 : 6,
                        StandardDocumentId = standardDocumentId,
                        EditDate = DateTime.Now,
                        SellerGln = sellerCodeToAcknowledge,
                        OrderType = orderTypeCode,
                        ShipToGln = keyValuePair.Key
                    };
                    ProcessOrderResponse(original, orderForResponse, orderResponse, supplier, alreadyRespondedTo);
                    alreadyRespondedTo = true;


                }
            }
            else
            {
                orderResponse = new OrderResponse
                {
                    Identifier = orderNumber,
                    XmlFile = order.ToString(),
                    ResponseXmlFile = newlyBuilt,
                    StatusId = 3,
                    StandardDocumentId = standardDocumentId,
                    EditDate = DateTime.Now,
                    SellerGln = sellerCodeToAcknowledge,
                    OrderType = orderTypeCode,
                    ShipToGln = shipToGln
                };

                ProcessOrderResponse(original, orderForResponse, orderResponse, supplier, false);
            }

            if (supplier.SendPdfToBizOrderCodes.Contains(orderTypeCode))
            {
                try
                {
                    var pdf = Generate.PdfBase64String(order, code);

                    if (supplier.SendPdf == true)
                    {
                        var biz = new BizIntegrationManager();
                        biz.SendPdf(pdf, order, code, supplier);
                    }
                }
                catch (Exception ex)
                {
                    gateway.LogError(orderNumber, ex.Message?.ToString() + "-" + ex.InnerException?.ToString(), supplier.Id);
                }
            }

        }

        private void ProcessOrderResponse(XDocument original, XElement order, OrderResponse orderResponse, Supplier supplier, bool alreadyRespondedTo = false)
        {
            MessagesGateway gateway = new MessagesGateway();
            var orderId = gateway.SaveOrder(orderResponse);

            var standardBusinessDocumentHeader = GetStandardBusinessDocumentHeader(original);

            var repondOrder = BuildRespondOrder(standardBusinessDocumentHeader, order, supplier);

            string orderResponseResponse = string.Empty;
            if (alreadyRespondedTo == false)
            {
                orderResponseResponse = SendOrderResponse(repondOrder, supplier);
            }

            if (string.IsNullOrEmpty(orderResponseResponse))
            {
                orderResponseResponse = "Doc already responded to.";
            }

            gateway.UpdateOrder(orderResponseResponse, orderId, repondOrder.ToString());
        }
    }
}