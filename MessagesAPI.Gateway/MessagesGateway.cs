using MessagesAPI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Remoting.Contexts;


namespace MessagesAPI.Gateway
{
    public class MessagesGateway
    {
        private static MessagesGateway _Instance;

        public MessagesGateway() { }

        public static MessagesGateway Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new MessagesGateway();
                }

                return _Instance;
            }
        }

        public List<Models.Supplier> GetSuppliers()
        {
            using (BusinessEngine context = new BusinessEngine())
            {

                List<Models.Supplier> suppliers = (from s in context.Suppliers
                                                   where s.IsEnabled == true
                                                   select new Models.Supplier
                                                   {
                                                       Id = s.Id,
                                                       AccountKey = s.AccountKey,
                                                       Ean = s.Ean,
                                                       Name = s.Name,
                                                       FileNamePrefix = s.FileNamePrefix,
                                                       AcceptRevisedAllocationOrders = s.AcceptRevisedAllocationOrders,
                                                       AcceptRevisedNormalOrders = s.AcceptRevisedNormalOrders,
                                                       UserName = s.UserName,
                                                       Password = s.Password,
                                                       ApiName = s.API.Name,
                                                       ApiEndPoint = s.API.EndPoint,
                                                       ApiNameSpace = s.API.NameSpace,
                                                       SendEmailAddress = s.SendEmailAddress,
                                                       DoSendEmails = s.DoSendEmails,
                                                       SendPdf = s.DoSendPdfs,
                                                       ApiGln = s.API.Gln,
                                                       NormalOrderCodes = s.API.NormalOrderCodes,
                                                       MultiStoreOrderCodes = s.API.MultiStoreOrderCodes,
                                                       RejectOrderCodes = s.API.RejectOrderCodes,
                                                       SendPdfToBizOrderCodes = s.API.SendPdfToBizOrderCodes,
                                                       UseAccountKey = s.UseAccountKey
                                                   }).ToList();

                return suppliers;

            }
        }

        public PostData GetPostData(string orderNumber, string sellerGln, string receiverGln)
        {
            using (BusinessEngine db = new BusinessEngine())
            {
                var pd = (from o in db.Orders
                          join sbd in db.StandardDocuments on o.StandardDocumentId equals sbd.Id
                          join s in db.Suppliers on sbd.SupplierId equals s.Id
                          join api in db.APIs on s.API_Id equals api.Id
                          where o.Identifier == orderNumber && api.Gln == receiverGln
                          select new PostData
                          {
                              Id = api.Id,
                              EndPoint = api.EndPoint,
                              Gln = api.Gln,
                              Name = api.Name,
                              NameSpace = api.NameSpace,
                              Password = s.Password,
                              UserName = s.UserName,
                              SellerGln = o.SellerGln
                          });



                if (pd.FirstOrDefault() == null)
                {
                    pd = from s in db.Suppliers
                         join api in db.APIs on s.API_Id equals api.Id
                         where s.Ean == receiverGln
                         select new PostData
                         {
                             Id = api.Id,
                             EndPoint = api.EndPoint,
                             Gln = api.Gln,
                             Name = api.Name,
                             NameSpace = api.NameSpace,
                             Password = s.Password,
                             UserName = s.UserName,
                             SellerGln = null
                         };
                }


                if (pd.FirstOrDefault() == null)
                {
                    pd = from s in db.Suppliers
                         join api in db.APIs on s.API_Id equals api.Id
                         where s.Ean == sellerGln
                         select new PostData
                         {
                             Id = api.Id,
                             EndPoint = api.EndPoint,
                             Gln = api.Gln,
                             Name = api.Name,
                             NameSpace = api.NameSpace,
                             Password = s.Password,
                             UserName = s.UserName,
                             SellerGln = null
                         };
                }


                return pd.FirstOrDefault();
            }
        }

        public Models.Supplier GetSupplier(int id)
        {
            var supplier = new Models.Supplier();

            using (BusinessEngine context = new BusinessEngine())
            {
                return (from s in context.Suppliers
                        where s.Id == id
                        select new Models.Supplier
                        {
                            Id = s.Id,
                            Name = s.Name,
                            AccountKey = s.AccountKey,
                            Ean = s.Ean,
                            UserName = s.UserName,
                            Password = s.Password,
                            FileNamePrefix = s.FileNamePrefix,
                            SupplierVendors = (from sv in context.SupplierVendors
                                               where sv.SupplierId == s.Id
                                               select new Models.SupplierVendor

                                               {
                                                   Id = sv.Id,
                                                   Code = sv.Code,
                                                   Ean = sv.Ean,
                                                   SellerEan = sv.SellerEan

                                               }).ToList(),
                            AcceptRevisedAllocationOrders = s.AcceptRevisedAllocationOrders,
                            AcceptRevisedNormalOrders = s.AcceptRevisedNormalOrders,
                            ApiName = s.API.Name,
                            ApiEndPoint = s.API.EndPoint,
                            ApiNameSpace = s.API.NameSpace,
                            SendEmailAddress = s.SendEmailAddress,
                            DoSendEmails = s.DoSendEmails,
                            SendPdf = s.DoSendPdfs,
                            ApiGln = s.API.Gln,
                            NormalOrderCodes = s.API.NormalOrderCodes,
                            MultiStoreOrderCodes = s.API.MultiStoreOrderCodes,
                            RejectOrderCodes = s.API.RejectOrderCodes,
                            SendPdfToBizOrderCodes = s.API.SendPdfToBizOrderCodes,
                            UseAccountKey = s.UseAccountKey
                        }).FirstOrDefault();
            }
        }

        public long SaveStandardDocument(string orderNumbers, string documentXml, int supplierId)
        {
            using (BusinessEngine context = new BusinessEngine())
            {
                var standardDocument = new StandardDocument
                {
                    SupplierId = supplierId,
                    OrderNumbers = orderNumbers,
                    StandardDocumentXml = documentXml,
                    CreatedDate = DateTime.Now
                };

                standardDocument.Id = context.StandardDocuments.Where(sd => sd.SupplierId == supplierId && sd.OrderNumbers == orderNumbers).Select(d => d.Id).FirstOrDefault();
                if (standardDocument.Id != 0) return -1;

                context.StandardDocuments.Add(standardDocument);
                context.SaveChanges();

                return standardDocument.Id;
            }
        }

        public long SaveOrder(OrderResponse orderResponse)
        {
            using (BusinessEngine context = new BusinessEngine())
            {
                var existingOrder = from o in context.Orders
                                    where o.Identifier == orderResponse.Identifier
                                        && o.StandardDocumentId == orderResponse.StandardDocumentId
                                        && o.SellerGln == orderResponse.SellerGln
                                        && o.OrderType == orderResponse.OrderType
                                        && o.ShipToGln == orderResponse.ShipToGln
                                    select o;


                if (existingOrder.FirstOrDefault() != null)
                {
                    var order = existingOrder.FirstOrDefault();
                    order.DoResend = false;
                    order.ResendDate = DateTime.Now;

                    context.SaveChanges();

                    return order.Id;
                }
                else
                {
                    var order = new Order
                    {
                        Identifier = orderResponse.Identifier,
                        XmlFile = orderResponse.XmlFile.ToString(),
                        EditDate = orderResponse.EditDate,
                        StatusId = orderResponse.StatusId,
                        StandardDocumentId = orderResponse.StandardDocumentId,
                        SellerGln = orderResponse.SellerGln,
                        OrderType = orderResponse.OrderType,
                        ShipToGln = orderResponse.ShipToGln
                    };

                    context.Orders.Add(order);
                    context.SaveChanges();

                    return order.Id;
                }



            }
        }

        public void UpdateOrder(string acknowledgementXml, long orderId, string responseXml)
        {
            using (BusinessEngine context = new BusinessEngine())
            {
                var order = (from o in context.Orders
                             where o.Id == orderId
                             select o).FirstOrDefault();

                order.AcknowledgementXml = acknowledgementXml;
                order.ResponseXmlFile = responseXml;
                order.DoResend = false;

                context.SaveChanges();
            }
        }

        public void LogError(long orderId, string errorMessage, int supplierId)
        {
            using (BusinessEngine context = new BusinessEngine())
            {
                var error = new Error
                {
                    OrderId = orderId,
                    ErrorMessage = errorMessage,
                    SupplierId = supplierId
                };

                context.Errors.Add(error);

                context.SaveChanges();
            }
        }

        public bool IsDuplicate(string orderNumber, string orderType, string shipToGln, int supplierId, bool isNormalOrderCode)
        {
            // TODO: Check DoResend
            using (BusinessEngine context = new BusinessEngine())
            {
                IQueryable<Order> orders = from o in context.Orders
                                           join d in context.StandardDocuments on o.StandardDocumentId equals d.Id
                                           where (o.Identifier == orderNumber && d.SupplierId == supplierId
                                           && o.ShipToGln == shipToGln)
                                           select o;

                Order order = null;
                if (isNormalOrderCode)
                {
                    order = orders.Where(o => o.OrderType == orderType).FirstOrDefault();
                }
                else
                {
                    order = orders.FirstOrDefault();
                }


                if (order == null || order?.DoResend == true)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public void SaveUnknownVendorCode(string code, Models.Supplier supplier)
        {
            using (BusinessEngine context = new BusinessEngine())
            {
                var supplierVendor = new SupplierVendor
                {
                    Code = code,
                    SupplierId = supplier.Id,
                    Ean = supplier.Ean,
                    SellerEan = supplier.Ean,
                    SystemAdded = true
                };

                context.SupplierVendors.Add(supplierVendor);

                context.SaveChanges();
            }
        }

        public List<Order> GetOrderResends()
        {
            using (BusinessEngine context = new BusinessEngine())
            {
                var orders = (from o in context.Orders
                              where o.DoResend == true
                              select o
                             ).ToList();

                return orders;
            }
        }

        public List<StandardDocument> StandardDocuments()
        {
            using (BusinessEngine context = new BusinessEngine())
            {
                var documents = (from d in context.StandardDocuments
                                 where d.DoResend == true
                                 select d
                             ).ToList();

                return documents;
            }
        }

        public void UpdateStandardDocument(long id)
        {
            using (BusinessEngine context = new BusinessEngine())
            {
                var document = (from d in context.StandardDocuments
                                where d.Id == id
                                select d).FirstOrDefault();

                document.DoResend = false;

                context.SaveChanges();
            }
        }

        public string GetSupplierGln(Invoice invoice)
        {
            using (BusinessEngine context = new BusinessEngine())
            {
                return (context.GetSellerGln(invoice.OrderNumber, invoice.SellerGln).FirstOrDefault());
            }
        }

        public Code GetReceiverSellerGln(string sellerCode, int supplierId)
        {
            using (BusinessEngine context = new BusinessEngine())
            {
                return (from vendor in context.SupplierVendors
                        where vendor.Code == sellerCode &&
                          vendor.SupplierId == supplierId
                        select new Code
                        {
                            ReceiverGln = vendor.Ean,
                            SellerGln = vendor.SellerEan,
                            Value = sellerCode,
                            Name = vendor.Vendor
                        }
                   ).FirstOrDefault();
            }
        }

        public void LogError(string orderNumber, string message, int supplierId)
        {
            using (BusinessEngine context = new BusinessEngine())
            {
                context.Errors.Add(
                    new Error
                    {
                        SupplierId = supplierId,
                        ErrorMessage = message,
                        Identifier = orderNumber,
                        OccuredAt = DateTime.Now
                    });

                context.SaveChanges();
            }
        }
    }
}
