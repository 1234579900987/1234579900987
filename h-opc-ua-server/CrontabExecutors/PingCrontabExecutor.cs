using Hylasoft.Opc.Base;
using Hylasoft.Opc.Common;
using Hylasoft.Opc.Crontabs;
using Hylasoft.Opc.Da;
using Hylasoft.Opc.Ua;
using SD.Infrastructure.CrontabBase;
using SD.Infrastructure.CrontabBase.Mediator;
using SD.IOC.Core.Mediators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hylasoft.Opc.CrontabExecutors
{
    /// <summary>
    /// Ping定时任务执行者
    /// </summary>
    public class PingCrontabExecutor : CrontabExecutor<PingCrontab>
    {
        /// <summary>
        /// 同步锁
        /// </summary>
        private static readonly object _Sync = new object();

        /// <summary>
        /// 执行任务
        /// </summary>
        /// <param name="crontab">定时任务</param>
        public override void Execute(PingCrontab crontab)
        {
            ConnectionStatus status = this.Connect(crontab.AdapteeServerAddress);

            if (status == ConnectionStatus.Success)
            {
                //适配
                IEnumerable<IAdapter> adapters = ResolveMediator.ResolveAll<IAdapter>();
                foreach (IAdapter adapter in adapters.Where(x => x.AdapteeServerAddress == crontab.AdapteeServerAddress))
                {
                    adapter.Adapt();
                }
            }
            if (status == ConnectionStatus.Success || status == ConnectionStatus.Connected)
            {
                //暂停Ping
                ScheduleMediator.Pause(crontab.AdapteeServerAddress + typeof(PingCrontab).Name);

                //恢复巡检
                ScheduleMediator.Resume(crontab.AdapteeServerAddress + typeof(PatrolCrontab).Name);
            }
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        private ConnectionStatus Connect(string url)
        {
            lock (_Sync)
            {
                //判断可用的客户端是否已存在
                if (Global.Clients.ContainsKey(url) && Global.Clients[url].Status == OpcStatus.Connected)
                {
                    //已连接
                    return ConnectionStatus.Connected;
                }

                OpcMode opcMode = Constants.AdapteeServerAddresses[url];
                Uri uri = new Uri(url);

                IOpcClient opcClient;
                if (opcMode == OpcMode.UA)
                {
                    opcClient = new UaClient(uri);
                }
                else if (opcMode == OpcMode.DA)
                {
                    opcClient = new DaClient(uri);
                }
                else
                {
                    throw new NotImplementedException("未知OPC模式");
                }

                try
                {
                    bool succeed = Task.Run(() =>
                    {
                        opcClient.Connect();

                        if (Global.Clients.ContainsKey(url))
                        {
                            Global.Clients[url].Dispose();
                            Global.Clients[url] = opcClient;
                        }
                        else
                        {
                            Global.Clients.Add(url, opcClient);
                        }
                    }).Wait(new TimeSpan(0, 0, 0, 5));

                    if (succeed)
                    {
                        //连接成功
                        return ConnectionStatus.Success;
                    }

                    throw new TimeoutException($"连接服务器\"{url}\"超时！");
                }
                catch
                {
                    //暂停巡检
                    ScheduleMediator.Pause(url + typeof(PatrolCrontab).Name);

                    //连接失败
                    return ConnectionStatus.Failed;
                }
            }
        }
    }
}
