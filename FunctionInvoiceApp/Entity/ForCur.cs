using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionInvoiceApp.Entity
{
    public class ForCur
    {
        public string Currency { get; set; }
        public double ConvRate { get; set; }
        public double ForexAmt { get; set; }
    }
}
