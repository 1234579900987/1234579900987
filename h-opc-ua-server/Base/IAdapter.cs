namespace Hylasoft.Opc.Base
{
    /// <summary>
    /// 适配器接口
    /// </summary>
    public interface IAdapter
    {
        /// <summary>
        /// 被适配服务器地址
        /// </summary>
        string AdapteeServerAddress { get; }

        /// <summary>
        /// 适配
        /// </summary>
        void Adapt();
    }
}
