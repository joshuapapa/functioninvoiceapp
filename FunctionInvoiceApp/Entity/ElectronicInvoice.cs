using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionInvoiceApp.Entity
{
    public class ElectronicInvoice
    {
        public string CompInvoiceId { get; set; }
        public string IssueDtm { get; set; }
        public string EisUniqueId { get; set; }
        public string DocType { get; set; }
        public string TransClass { get; set; }
        public string CorrYN { get; set; }
        public string CorrectionCd { get; set; }
        public string PrevUniqueId { get; set; }
        public string Rmk1 { get; set; }
        public SellerInfo SellerInfo { get; set; }
        public BuyerInfo BuyerInfo { get; set; }
        public List<ItemList> ItemList { get; set; }
        public double TotNetItemSales { get; set; }
        public Discount Discount { get; set; }
        public double OtherTaxRev { get; set; }
        public double TotNetSalesAftDisct { get; set; }
        public double VATAmt { get; set; }
        public double WithholdIncome { get; set; }
        public double WithholdBusVAT { get; set; }
        public double WithholdBusPT { get; set; }
        public double OtherNonTaxCharge { get; set; }
        public double NetAmtPay { get; set; }
        public ForCur ForCur { get; set; }
        public string PtuNum { get; set; }
    }
}
