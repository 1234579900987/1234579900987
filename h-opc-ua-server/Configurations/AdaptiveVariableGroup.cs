using Opc.Ua;
using System.Collections.Generic;

namespace Hylasoft.Opc.Configurations
{
    /// <summary>
    /// 适配变量组
    /// </summary>
    public class AdaptiveVariableGroup : FolderState
    {
        /// <summary>
        /// 创建适配变量组构造器
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="description">描述</param>
        public AdaptiveVariableGroup(string name, string description)
            : base(null)
        {
            base.SymbolicName = name;
            base.ReferenceTypeId = ReferenceTypes.Organizes;
            base.TypeDefinitionId = ObjectTypeIds.FolderType;
            base.Description = description;
            base.NodeId = new NodeId(name);
            base.BrowseName = new QualifiedName(name);
            base.DisplayName = new LocalizedText(name);
            base.WriteMask = AttributeWriteMask.None;
            base.UserWriteMask = AttributeWriteMask.None;
            base.EventNotifier = EventNotifiers.None;
        }

        /// <summary>
        /// 适配变量列表
        /// </summary>
        public IEnumerable<AdaptiveVariable> AdaptiveVariables { get; set; }
    }
}