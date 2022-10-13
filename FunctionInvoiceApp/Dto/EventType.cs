using System.Runtime.Serialization;

namespace FunctionInvoiceApp.DTO
{

    public enum EventType{
        [EnumMember(Value = "Create")]
        CREATE = 1,
        [EnumMember(Value = "Update")]
        UPDATE = 2
    }

}
