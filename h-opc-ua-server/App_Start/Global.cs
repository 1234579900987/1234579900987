using Hylasoft.Opc.Common;
using Hylasoft.Opc.Configurations;
using Hylasoft.Opc.Crontabs;
using Microsoft.Extensions.DependencyInjection;
using Opc.Ua;
using SD.Infrastructure.CrontabBase.Mediator;
using SD.IOC.Core.Mediators;
using SD.IOC.Extension.NetFx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Hylasoft.Opc
{
    /// <summary>
    /// 全局操作类
    /// </summary>
    public static class Global
    {
        /// <summary>
        /// 适配变量组字典
        /// </summary>
        private static IDictionary<NodeId, AdaptiveVariableGroup> _AdaptiveVariableGroups;

        /// <summary>
        /// 适配变量组字典
        /// </summary>
        public static IDictionary<NodeId, AdaptiveVariableGroup> AdaptiveVariableGroups
        {
            get { return Global._AdaptiveVariableGroups; }
        }

        /// <summary>
        /// 适配变量字典
        /// </summary>
        private static IDictionary<NodeId, AdaptiveVariable> _AdaptiveVariables;

        /// <summary>
        /// 适配变量字典
        /// </summary>
        public static IDictionary<NodeId, AdaptiveVariable> AdaptiveVariables
        {
            get { return Global._AdaptiveVariables; }
        }

        /// <summary>
        /// OPC客户端字典
        /// </summary>
        public static readonly IDictionary<string, IOpcClient> Clients = new ConcurrentDictionary<string, IOpcClient>();

        /// <summary>
        /// 初始化依赖注入
        /// </summary>
        public static void InitializeDependencies()
        {
            if (!ResolveMediator.ContainerBuilt)
            {
                IServiceCollection builder = ResolveMediator.GetServiceCollection();
                builder.RegisterConfigs();

                ResolveMediator.Build();
            }
        }

        /// <summary>
        /// 初始化配置
        /// </summary>
        public static void InitializeConfiguration()
        {
            XDocument xDocument = XDocument.Load(Constants.VariableConfigPath);
            IEnumerable<XElement> groupElements = xDocument.Element(Constants.RootPath)?.Elements() ?? new XElement[0];

            IList<AdaptiveVariableGroup> variableGroups = new List<AdaptiveVariableGroup>();

            foreach (XElement groupElement in groupElements)
            {
                string groupName = groupElement.Attribute(Constants.NameAttribute)?.Value;
                string groupDescription = groupElement.Attribute(Constants.DescriptionAttribute)?.Value;

                AdaptiveVariableGroup variableGroup = new AdaptiveVariableGroup(groupName, groupDescription);

                IList<AdaptiveVariable> variables = new List<AdaptiveVariable>();
                foreach (XElement varElement in groupElement.Elements())
                {
                    string variableName = varElement.Attribute(Constants.NameAttribute)?.Value;
                    string variableDescription = varElement.Attribute(Constants.DescriptionAttribute)?.Value;

                    AdaptiveVariable variable = new AdaptiveVariable(variableName, variableDescription, variableGroup);
                    variables.Add(variable);
                }

                variableGroup.AdaptiveVariables = variables;
                variableGroups.Add(variableGroup);
            }

            Global._AdaptiveVariableGroups = variableGroups.ToDictionary(x => x.NodeId, x => x);
            Global._AdaptiveVariables = variableGroups.SelectMany(x => x.AdaptiveVariables).ToDictionary(x => x.NodeId, x => x);
        }

        /// <summary>
        /// 初始化应用程序
        /// </summary>
        public static void InitializeApplication()
        {
            //清理
            Global.FinalizeApplication();

            foreach (string url in Constants.AdapteeServerAddresses.Keys)
            {
                //Ping服务器
                PingCrontab pingCrontab = new PingCrontab(url, 10);
                ScheduleMediator.Schedule(pingCrontab);

                //巡检服务器
                PatrolCrontab patrolCrontab = new PatrolCrontab(url, 10);
                ScheduleMediator.Schedule(patrolCrontab);
            }
        }

        /// <summary>
        /// 清理应用程序
        /// </summary>
        public static void FinalizeApplication()
        {
            //清空定时任务
            ScheduleMediator.Clear();

            Task.Run(() =>
            {
                //释放OPC客户端
                foreach (IOpcClient opcClient in Global.Clients.Values)
                {
                    opcClient.Dispose();
                }
            }).Wait(new TimeSpan(0, 0, 0, 5));
        }
    }
}
