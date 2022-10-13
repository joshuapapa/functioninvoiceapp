using FunctionInvoiceApp.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionInvoiceApp.Dto
{
    public class Payload
    {
        public List<PayloadEvent> Events { get; set; }
        public int LastEventSequence { get; set; }
        public int FirstEventSequence { get; set; }
        public string Entropy { get; set; }
    }
}
