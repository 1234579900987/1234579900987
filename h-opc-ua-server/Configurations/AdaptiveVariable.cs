using Opc.Ua;
using System;

namespace Hylasoft.Opc.Configurations
{
    /// <summary>
    /// 适配变量
    /// </summary>
    public class AdaptiveVariable : BaseDataVariableState
    {
        /// <summary>
        /// 创建适配变量构造器
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="description">描述</param>
        /// <param name="variableGroup">变量组</param>
        public AdaptiveVariable(string name, string description, AdaptiveVariableGroup variableGroup)
            : base(variableGroup)
        {
            base.SymbolicName = name;
            base.ReferenceTypeId = ReferenceTypes.Organizes;
            base.TypeDefinitionId = VariableTypeIds.BaseDataVariableType;
            base.NodeId = new NodeId($"{variableGroup.SymbolicName}.{name}");
            base.BrowseName = new QualifiedName(name);
            base.DisplayName = new LocalizedText(name);
            base.Description = new LocalizedText(description);
            base.WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            base.UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            base.DataType = DataTypeIds.BaseDataType;
            base.ValueRank = ValueRanks.Scalar;
            base.AccessLevel = AccessLevels.CurrentReadOrWrite;
            base.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            base.Historizing = false;
            base.Value = null;
            base.StatusCode = StatusCodes.Good;
            base.Timestamp = DateTime.Now;

            variableGroup.AddChild(this);
        }

        /// <summary>
        /// 编号
        /// </summary>
        public string Number
        {
            get { return $"{this.VariableGroup.SymbolicName}.{this.SymbolicName}"; }
        }

        /// <summary>
        /// 变量组
        /// </summary>
        public AdaptiveVariableGroup VariableGroup
        {
            get { return (AdaptiveVariableGroup)this.Parent; }
        }

        /// <summary>
        /// 赋值
        /// </summary>
        /// <param name="value">值</param>
        public void SetValue(object value)
        {
            base.Value = value;
            base.ClearChangeMasks(Base.OpcServiceHost.NodeManager.SystemContext, false);
        }
    }
}
