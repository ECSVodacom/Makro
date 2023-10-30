using MessagesAPI.Models;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MessagesAPI.Pdf
{
    public class Generate
    {
        public static string PdfBase64String(XElement order, Code code)
        {
            var shipToGln = order.Element("orderLineItem")?.Element("orderLineItemDetail").
               Element("orderLogisticalInformation")?.Element("shipTo")?.Element("gln").Value ??
               string.Empty; // "[Ship To Gln Missing]";
            var shipToDescription = order.Element("orderLineItem")?.Element("orderLineItemDetail")?.
               Element("orderLogisticalInformation")?.Element("shipTo")?.
               Element("additionalPartyIdentification")?.Value ?? string.Empty;//"[Ship To Description Missing]";

            var orderTotal = string.Format("{0:N2}", order.Element("totalMonetaryAmountExcludingTaxes")?.Value);
            var orderNumber = order.Element("orderIdentification").Element("entityIdentification")?.Value;
            var orderLineItem = order.Elements("orderLineItem")?.FirstOrDefault();
            var orderTypeCode = order.Element("orderTypeCode")?.Value == string.Empty ?
             order.Element("orderTypeCode")?.Attribute("codeListVersion")?.Value :
             order.Element("orderTypeCode")?.Value;

            //if (orderTypeCode == "")
            //{
            //    orderTypeCode = "[Order Type Missing]";
            //}

            var deliveryDate = Convert.ToDateTime(orderLineItem.Element("orderLineItemDetail")
                ?.Element("orderLogisticalInformation")
                ?.Element("orderLogisticalDateInformation")
                ?.Element("requestedDeliveryDateTime")
                ?.Element("date").Value);
            var deliveryDateString = deliveryDate.ToString("yyyy-MM-dd");

            var orderDate = Convert.ToDateTime(order.Element("creationDateTime").Value);
            var orderDateString = orderDate.ToString("yyyy-MM-dd");
            string headerNarrative = string.Empty;

            Document pdfDocument = new Document();
            Section section = pdfDocument.AddSection();
            section.AddParagraph($"Customer Purchase Order Copy Generated from EDI interface");
            section.AddParagraph();
            section.AddParagraph($"Customer Name: {shipToGln} - {shipToDescription}");
            section.AddParagraph();
            section.AddParagraph($"{code.SellerGln} - {code.Value} - {code.SellerGln} - {code.Name}");
            section.AddParagraph();
            section.AddParagraph($"Order Date: {orderDateString}");
            section.AddParagraph($"PO Number: {orderNumber} {orderTypeCode}");
            section.AddParagraph($"Order Total: {orderTotal}");
            section.AddParagraph($"Delivery Date: {deliveryDateString}");
            section.AddParagraph();

            Table table = AddTable(section);
            SetColumns(table);
            SetTableHeaders(table);

            var orderLineItems = order.Descendants("orderLineItem").OrderBy(e => Convert.ToInt32(e.Element("lineItemNumber").Value));
            foreach (var li in orderLineItems)
            {
                var lineNarrative = string.Empty;
                var additionalTradeItemIdentification = li.Elements("transactionalTradeItem").Elements("additionalTradeItemIdentification");
                var supplierProductCode = from el in additionalTradeItemIdentification
                                          where (string)el.Attribute("additionalTradeItemIdentificationTypeCode") == "SUPPLIER_ASSIGNED_ITEMID"
                                          select el.Value;
                var customerProductCode = from el in additionalTradeItemIdentification
                                          where (string)el.Attribute("additionalTradeItemIdentificationTypeCode") == "BUYER_ASSIGNED_ITEMID"
                                          select el.Value;

                Row row = table.Rows.AddRow();
                row.Format.Font.Size = 8;
                row.Cells[0].AddParagraph(li.Element("lineItemNumber").Value);
                row.Cells[0].Format.Alignment = ParagraphAlignment.Center;
                row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[1].AddParagraph((supplierProductCode?.FirstOrDefault().Length > 0 ? supplierProductCode?.FirstOrDefault() : customerProductCode.FirstOrDefault())); // "[Product Code Not Supplied]");
                row.Cells[1].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[2].AddParagraph(li?.Element("transactionalTradeItem")?.Element("gtin")?.Value);
                row.Cells[2].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[3].AddParagraph(li?.Element("transactionalTradeItem")?.Element("tradeItemDescription")?.Value);
                row.Cells[3].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[4].AddParagraph(li?.Element("transactionalTradeItem")?.Element("size")?.Element("sizeCode")?.Value ?? string.Empty);
                row.Cells[4].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[5].AddParagraph(string.Format("{0:N2}", Convert.ToDecimal(li?.Element("requestedQuantity")?.Value, CultureInfo.InvariantCulture)));
                row.Cells[5].Format.Alignment = ParagraphAlignment.Center;
                row.Cells[5].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[6].AddParagraph(string.Format("{0:N2}", li?.Element("netPrice")?.Value, CultureInfo.InvariantCulture));
                row.Cells[6].Format.Alignment = ParagraphAlignment.Right;
                row.Cells[6].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[7].AddParagraph(string.Format("{0:N2}", li?.Element("monetaryAmountExcludingTaxes")?.Value, CultureInfo.InvariantCulture));
                row.Cells[7].Format.Alignment = ParagraphAlignment.Right;
                row.Cells[7].VerticalAlignment = VerticalAlignment.Center;
            }

            PdfDocumentRenderer renderer = new PdfDocumentRenderer(false);
            renderer.Document = pdfDocument;
            renderer.RenderDocument();

            using (var stream = new MemoryStream())
            {
                renderer.PdfDocument.Save(stream);
                var base64String = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length);
                return base64String;
            }
        }

        private static Table AddTable(Section section)
        {
            var table = section.AddTable();
            table.Style = "Table";
            table.Borders.Color = Color.Empty;
            table.Borders.Width = 0.25;
            table.Borders.Left.Width = 0.5;
            table.Borders.Right.Width = 0.5;
            table.Rows.LeftIndent = 0;
            return table;
        }

        private static void SetColumns(Table table)
        {
            Column column = table.AddColumn("1cm");
            column.Format.Alignment = ParagraphAlignment.Left;
            column = table.AddColumn("3cm");
            column.Format.Alignment = ParagraphAlignment.Left;
            column = table.AddColumn("3cm");
            column.Format.Alignment = ParagraphAlignment.Left;
            column = table.AddColumn("4cm");
            column.Format.Alignment = ParagraphAlignment.Left;
            column = table.AddColumn("1cm");
            column.Format.Alignment = ParagraphAlignment.Left;
            column = table.AddColumn("2cm");
            column.Format.Alignment = ParagraphAlignment.Left;
            column = table.AddColumn("2cm");
            column.Format.Alignment = ParagraphAlignment.Left;
            column = table.AddColumn("2cm");
            column.Format.Alignment = ParagraphAlignment.Left;
        }

        private static void SetTableHeaders(Table table)
        {
            Row row = table.AddRow();
            row.HeadingFormat = true;
            row.Format.Alignment = ParagraphAlignment.Center;
            row.VerticalAlignment = VerticalAlignment.Center;
            row.Shading.Color = Color.Empty;
            row.Cells[0].AddParagraph("Line Item");
            row.Cells[0].Format.Alignment = ParagraphAlignment.Center;
            row.Cells[1].AddParagraph("Product Code");
            row.Cells[1].Format.Alignment = ParagraphAlignment.Center;
            row.Cells[2].AddParagraph("Barcode");
            row.Cells[2].Format.Alignment = ParagraphAlignment.Center;
            row.Cells[3].AddParagraph("Product Description");
            row.Cells[3].Format.Alignment = ParagraphAlignment.Center;
            row.Cells[4].AddParagraph("UOM");
            row.Cells[4].Format.Alignment = ParagraphAlignment.Center;
            row.Cells[5].AddParagraph("Qty");
            row.Cells[5].Format.Alignment = ParagraphAlignment.Center;
            row.Cells[6].AddParagraph("Price");
            row.Cells[6].Format.Alignment = ParagraphAlignment.Center;
            row.Cells[7].AddParagraph("Line Cost");
            row.Cells[7].Format.Alignment = ParagraphAlignment.Center;
        }
    }
}