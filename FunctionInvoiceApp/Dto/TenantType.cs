using System.Runtime.Serialization;

namespace FunctionInvoiceApp.DTO
{
    public enum TenantType{
        [EnumMember(Value = "ORGANISATION")]
        ORGANISATION = 1
    }
}
