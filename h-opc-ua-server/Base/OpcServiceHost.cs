using Opc.Ua;
using Opc.Ua.Server;

namespace Hylasoft.Opc.Base
{
    /// <summary>
    /// OPC服务主机
    /// </summary>
    public class OpcServiceHost : StandardServer
    {
        /// <summary>
        /// 节点管理器
        /// </summary>
        private static NodeManager _NodeManager;

        /// <summary>
        /// 节点管理器
        /// </summary>
        public static NodeManager NodeManager
        {
            get { return OpcServiceHost._NodeManager; }
        }

        /// <summary>
        /// 创建主节点管理器
        /// </summary>
        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            OpcServiceHost._NodeManager = new NodeManager(server, configuration);
            return new MasterNodeManager(server, configuration, null, OpcServiceHost._NodeManager);
        }
    }
}
