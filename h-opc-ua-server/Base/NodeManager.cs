using Hylasoft.Opc.Configurations;
using Opc.Ua;
using Opc.Ua.Server;
using System.Collections.Generic;

namespace Hylasoft.Opc.Base
{
    /// <summary>
    /// 节点管理器
    /// </summary>
    public class NodeManager : CustomNodeManager2
    {
        /// <summary>
        /// 创建节点管理器构造器
        /// </summary>
        public NodeManager(IServerInternal server, ApplicationConfiguration configuration)
            : base(server, configuration, Namespaces.OpcUa)
        {
            base.SystemContext.NodeIdFactory = this;
        }

        /// <summary>
        /// 创建地址空间
        /// </summary>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (base.Lock)
            {
                base.LoadPredefinedNodes(base.SystemContext, externalReferences);

                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out IList<IReference> references))
                {
                    references = new List<IReference>();
                    externalReferences[ObjectIds.ObjectsFolder] = references;
                }

                foreach (AdaptiveVariableGroup adaptiveVariableGroup in Global.AdaptiveVariableGroups.Values)
                {
                    adaptiveVariableGroup.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
                    references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, adaptiveVariableGroup.NodeId));
                    adaptiveVariableGroup.EventNotifier = EventNotifiers.SubscribeToEvents;
                    base.AddRootNotifier(adaptiveVariableGroup);

                    base.AddPredefinedNode(base.SystemContext, adaptiveVariableGroup);
                }
            }
        }
    }
}
