using SD.Infrastructure.CrontabBase;
using System;

namespace Hylasoft.Opc.Crontabs
{
    /// <summary>
    /// 巡检定时任务
    /// </summary>
    public class PatrolCrontab : Crontab
    {
        #region # 构造器

        #region 01.基础构造器
        /// <summary>
        /// 基础构造器
        /// </summary>
        public PatrolCrontab(string url, int patrolInterval)
            : base(new TimeSpanStrategy(new TimeSpan(0, 0, patrolInterval)))
        {
            this.Id = url + typeof(PatrolCrontab).Name;
            this.AdapteeServerAddress = url;
            this.PatrolInterval = patrolInterval;
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

        #region 巡检间隔 —— int PatrolInterval
        /// <summary>
        /// 巡检间隔
        /// </summary>
        /// <remarks>单位：秒</remarks>
        public int PatrolInterval { get; set; }
        #endregion

        #endregion
    }
}
