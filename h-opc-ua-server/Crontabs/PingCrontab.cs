using SD.Infrastructure.CrontabBase;
using System;

namespace Hylasoft.Opc.Crontabs
{
    /// <summary>
    /// Ping定时任务
    /// </summary>
    public class PingCrontab : Crontab
    {
        #region # 构造器

        #region 01.基础构造器
        /// <summary>
        /// 基础构造器
        /// </summary>
        public PingCrontab(string url, int pingInterval)
            : base(new TimeSpanStrategy(new TimeSpan(0, 0, pingInterval)))
        {
            this.Id = url + typeof(PingCrontab).Name;
            this.AdapteeServerAddress = url;
            this.PingInterval = pingInterval;
        }
        #endregion 

        #endregion

        #region # 属性

        #region OPC服务器地址 —— string AdapteeServerAddress
        /// <summary>
        /// OPC服务器地址
        /// </summary>
        public string AdapteeServerAddress { get; set; }
        #endregion

        #region Ping间隔 —— int PingInterval
        /// <summary>
        /// Ping间隔
        /// </summary>
        /// <remarks>单位：秒</remarks>
        public int PingInterval { get; set; }
        #endregion

        #endregion
    }
}
