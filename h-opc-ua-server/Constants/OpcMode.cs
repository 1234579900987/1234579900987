using System.ComponentModel;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace Hylasoft.Opc
{
    /// <summary>
    /// OPC模式
    /// </summary>
    [DataContract]
    public enum OpcMode
    {
        /// <summary>
        /// 经典架构
        /// </summary>
        [EnumMember]
        [Description("DA")]
        DA = 0,

        /// <summary>
        /// 统一架构
        /// </summary>
        [EnumMember]
        [Description("UA")]
        UA = 1
    }
}
