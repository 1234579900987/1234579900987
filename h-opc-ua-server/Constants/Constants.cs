using System.Collections.Generic;


// ReSharper disable once CheckNamespace
namespace Hylasoft.Opc
{
    /// <summary>
    /// 常量
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// 服务器配置路径
        /// </summary>
        public const string ServerConfigPath = "App.Server.config";

        /// <summary>
        /// 变量配置路径
        /// </summary>
        public const string VariableConfigPath = "App.Variable.config";

        /// <summary>
        /// 变量配置根路径
        /// </summary>
        public const string RootPath = "OPC.UA.Server";

        /// <summary>
        /// 变量配置名称特性
        /// </summary>
        public const string NameAttribute = "name";

        /// <summary>
        /// 变量配置描述特性
        /// </summary>
        public const string DescriptionAttribute = "description";

        /// <summary>
        /// 被适配服务器地址列表
        /// </summary>
        public static IDictionary<string, OpcMode> AdapteeServerAddresses = new Dictionary<string, OpcMode>
        {
            {"opcda://192.168.0.153/Matrikon.OPC.Simulation.1", OpcMode.DA}
        };
    }
}
