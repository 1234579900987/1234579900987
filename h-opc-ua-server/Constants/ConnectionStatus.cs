using System.ComponentModel;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace Hylasoft.Opc
{
    /// <summary>
    /// 连接状态
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// 连接成功
        /// </summary>
        [EnumMember]
        [Description("连接成功")]
        Success = 0,

        /// <summary>
        /// 连接失败
        /// </summary>
        [EnumMember]
        [Description("连接失败")]
        Failed = 1,

        /// <summary>
        /// 已连接
        /// </summary>
        [EnumMember]
        [Description("已连接")]
        Connected = 2
    }
}