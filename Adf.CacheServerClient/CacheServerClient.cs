using System;
using System.Collections.Generic;
using System.Text;

namespace Adf
{
    /// <summary>
    /// 缓存客户端基类
    /// </summary>
    /// <remarks>本实例所有key传值不能为null或空字符中，value可以为null</remarks>
    public class CacheServerClient : CacheServerClientBase, Adf.ICache
    {
        /// <summary>
        /// 获取CacheServer配置实例
        /// </summary>
        public static CacheServerClient Instance = new CacheServerClient();

        private CacheServerClient()
            : base("CacheServer")
        {
        }
    }
}