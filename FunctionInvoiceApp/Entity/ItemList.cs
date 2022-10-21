using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionInvoiceApp.Entity
{
    public class ItemList
    {
        public string Nm { get; set; }
        public string Desc { get; set; }
        public double Qty { get; set; }
        public string Unit { get; set; }
        public double UnitCost { get; set; }
        public double SalesAmt { get; set; }
        public double RegDscntAmt { get; set; }
        public double SpeDscntAmt { get; set; }
        public double NetSales { get; set; }
    }
}
