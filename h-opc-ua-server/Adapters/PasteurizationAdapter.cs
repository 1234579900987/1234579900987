using Hylasoft.Opc.Base;
using Hylasoft.Opc.Common;
using Hylasoft.Opc.Configurations;
using System;

namespace Hylasoft.Opc.Adapters
{
    /// <summary>
    /// 巴氏杀菌适配器
    /// </summary>
    public class PasteurizationAdapter : IAdapter
    {
        /// <summary>
        /// 被适配服务器地址
        /// </summary>
        public string AdapteeServerAddress
        {
            get { return "opcda://192.168.0.153/Matrikon.OPC.Simulation.1"; }
        }

        /// <summary>
        /// 适配
        /// </summary>
        public void Adapt()
        {
            string testMonitor = "Pasteurization.TestMonitor";
            string testSwitch = "Pasteurization.TestSwitch";

            string @switch = "Pasteurization.Switch";

            AdaptiveVariable specVariable = Global.AdaptiveVariables[@switch];


            IOpcClient client = Global.Clients[this.AdapteeServerAddress];
            client.Monitor(new[] { testMonitor, testSwitch }, (readEvents, unsubscribe) =>
            {
                double value = Convert.ToDouble(readEvents[testMonitor].Value);
                bool on = Convert.ToBoolean(readEvents[testSwitch].Value);

                if (on && value > 0)
                {
                    specVariable.SetValue(true);
                }
                else
                {
                    specVariable.SetValue(false);
                }
            });
        }
    }
}
