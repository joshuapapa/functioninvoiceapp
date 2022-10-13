
using System.Runtime.Serialization;

namespace FunctionInvoiceApp.DTO
{
    public enum EventCategory{
        [EnumMember(Value = "CONTACT")]
        CONTACT = 1,
        [EnumMember(Value = "INVOICE")]
        INVOICE = 2
    }
}
