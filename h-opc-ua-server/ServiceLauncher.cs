using Hylasoft.Opc.Base;
using Opc.Ua;
using Opc.Ua.Configuration;
using System;

namespace Hylasoft.Opc
{
    /// <summary>
    /// 服务启动器
    /// </summary>
    public class ServiceLauncher
    {
        /// <summary>
        /// OPC应用实例
        /// </summary>
        private readonly ApplicationInstance _application;

        /// <summary>
        /// 构造器
        /// </summary>
        public ServiceLauncher()
        {
            this._application = new ApplicationInstance
            {
                ApplicationType = ApplicationType.Server,
                ConfigSectionName = Program.ServiceDisplayName
            };
        }

        /// <summary>
        /// 开始
        /// </summary>
        public void Start()
        {
            //加载配置
            this._application.LoadApplicationConfiguration(Constants.ServerConfigPath, false);

            //认证配置
            this._application.CheckApplicationInstanceCertificate(false, 0);

            //启动服务器
            OpcServiceHost serviceHost = new OpcServiceHost();
            this._application.Start(serviceHost);

            //初始化应用程序
            Global.InitializeApplication();

            Console.WriteLine("服务已启动...");
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            this._application.Stop();

            Console.WriteLine("服务已关闭...");
        }
    }
}
