using Hylasoft.Opc.Common;
using Hylasoft.Opc.Crontabs;
using Hylasoft.Opc.Da;
using Hylasoft.Opc.Ua;
using SD.Infrastructure.CrontabBase;
using SD.Infrastructure.CrontabBase.Mediator;
using System;
using System.Threading.Tasks;

namespace Hylasoft.Opc.CrontabExecutors
{
    /// <summary>
    /// 巡检定时任务执行者
    /// </summary>
    public class PatrolCrontabExecutor : CrontabExecutor<PatrolCrontab>
    {
        /// <summary>
        /// 执行任务
        /// </summary>
        /// <param name="crontab">定时任务</param>
        public override void Execute(PatrolCrontab crontab)
        {
            this.Patrol(crontab.AdapteeServerAddress);
        }

        /// <summary>
        /// 巡检
        /// </summary>
        private void Patrol(string url)
        {
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
                    //测试连接
                    opcClient.Connect();
                    opcClient.Dispose();

                }).Wait(new TimeSpan(0, 0, 0, 5));

                if (!succeed)
                {
                    throw new TimeoutException($"连接服务器\"{url}\"超时！");
                }
            }
            catch
            {
                //释放客户端
                if (Global.Clients.ContainsKey(url))
                {
                    IOpcClient specClient = Global.Clients[url];
                    Global.Clients.Remove(url);
                    Task.Run(() =>
                    {
                        specClient.Dispose();
                    }).Wait(new TimeSpan(0, 0, 0, 2));
                }

                //暂停巡检
                ScheduleMediator.Pause(url + typeof(PatrolCrontab).Name);

                //重新Ping
                ScheduleMediator.Resume(url + typeof(PingCrontab).Name);
            }
        }
    }
}
