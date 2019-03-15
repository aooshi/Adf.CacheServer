using System;
using System.Collections.Generic;
using System.Text;

namespace Adf
{
    /// <summary>
    /// 缓存客户端基类, 该基础类对于对象默认使用 <see cref="Adf.DataSerializable.DefaultInstance"/> 序列化处理器，仅处理基础元素数据，若要支持自定义对象，请重戴<see cref="CreateSerializable"/>以设置新的序列化处理器
    /// </summary>
    /// <remarks>本实例所有key传值不能为null或空字符中，value可以为null</remarks>
    public abstract class CacheServerClientBase : Adf.Cs.Client, Adf.ICache
    {
        IBinarySerializable binarySerializable;

        /// <summary>
        /// 初始新实例，并指定配置节
        /// </summary>
        /// <param name="configName"></param>
        public CacheServerClientBase(string configName)
            : base("CacheServer", configName)
        {
            this.binarySerializable = this.CreateSerializable();
            if (this.binarySerializable == null)
                throw new NullReferenceException("CreateSerializable method return null");
        }

        /// <summary>
        /// 创建序列化处理器
        /// </summary>
        /// <returns></returns>
        protected virtual IBinarySerializable CreateSerializable()
        {
            return Adf.DataSerializable.DefaultInstance;
        }

        /// <summary>
        /// 删除一个缓存
        /// </summary>
        /// <param name="key"></param>
        public bool Delete(string key)
        {
            return base.HashCommand<bool>("Delete", key, key);
        }

        /// <summary>
        /// 获取缓存值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public byte[] Get(string key)
        {
            return base.HashCommand<byte[]>("Get", key, key);
        }

        /// <summary>
        /// 获取字符串表现形式的缓存值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetString(string key)
        {
            var data = base.HashCommand<byte[]>("Get", key, key);
            if (data == null)
            {
                return null;
            }
            return Encoding.UTF8.GetString(data);
        }

        /// 
        /// <summary>
        /// set cache, add new or replace exists,设置一个缓存值，键已存在则修改，不存在则添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="initExpires">new key expires,新键的初始过期时间</param>
        /// <param name="existsExpires">exists key expires,zero the no update,为已有键指定新的过期时间，零值不更新，单位：秒</param>
        /// <returns>0: error, 1: replace, 2: add</returns>
        public int Set(string key, byte[] value, int initExpires, int existsExpires)
        {
            return base.HashCommand<int>("Set", key, key, value, initExpires, existsExpires);
        }

        /// <summary>
        /// 设置一个缓存值，若缓存存在则覆盖
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="existsExpires"></param>
        /// <param name="initExpires"></param>
        /// <returns>0: error, 1: replace, 2: add</returns>
        public int Set(string key, string value, int initExpires, int existsExpires)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            var data = Encoding.UTF8.GetBytes(value);
            return base.HashCommand<int>("Set", key, key, data, initExpires, existsExpires);
        }

        /// <summary>
        /// 添加缓存，忽略已存在缓存项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expires"></param>
        /// <returns>success: true, failure or exists: false</returns>
        public bool Add(string key, string value, int expires)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            var data = Encoding.UTF8.GetBytes(value);
            return base.HashCommand<bool>("Add", key, key, data, expires);
        }

        /// <summary>
        /// 添加缓存，忽略已存在缓存项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expires"></param>
        /// <returns>success: true, failure or exists: false</returns>
        public bool Add(string key, byte[] value, int expires)
        {
            return base.HashCommand<bool>("Add", key, key, value, expires);
        }

        /// <summary>
        /// increment number，进行一次数据增量计算并返回计算后结果
        /// </summary>
        /// <param name="key"></param>
        /// <param name="num">Positive for add, negative for reduction，要进行计算的增量，正数为加，负数为减</param>
        /// <param name="initValue">new key init value,不存在键，则以该值进行初始化</param>
        /// <param name="initExpires">init expires,不存在键以该值进行过期时间初始化</param>
        /// <param name="existsExpires">exists expires, zero the no update,对已有数值增加时指定新的过期时间，零值不更新，单位：秒</param>
        /// <returns>new key return initValue, exists key return increment value,计算后的值</returns>
        public long Increment(string key, long num, long initValue, int initExpires, int existsExpires)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }
            return base.HashCommand<long>("Increment", key, key, num, initValue, initExpires, existsExpires);
        }

        /// <summary>
        /// increment number from exists key, 进行一次数据增量计算并返回计算后结果
        /// 此方法要添加的值，必需是已通过<see cref="Increment"/>方法增加过并未过期的值，否则将返回FALSE
        /// </summary>
        /// <param name="key"></param>
        /// <param name="num">Positive for add, negative for reduction,要进行计算的增量，正数为加，负数为减</param>
        /// <param name="existsExpires">exists expires, zero the no update,对已有数值增加时，附加的过期时间，零值不更新，单位：秒</param>
        /// <param name="newNum">num of increment,计算后的新值</param>
        /// <returns>find key and increment success: true,  other: false</returns>
        public bool IncrementEx(string key, long num, int existsExpires, out long newNum)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }
            newNum = 0;
            var param = new object[] { key, num, existsExpires, newNum };
            var result = base.HashCommand<bool>("IncrementEx", key, param);
            newNum = Convert.ToInt64(param[3]);
            return result;
        }
        
        /// <summary>
        /// 按定义的序列化方法获取缓存值的T对象实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            var data = this.Get(key);
            if (data == null)
            {
                return default(T);
            }

            var type = typeof(T);
            var obj = this.binarySerializable.Deserialize(type, data);
            return (T)obj;
        }

        /// <summary>
        /// 按定义的序列化方式获取缓存值的type对象实例
        /// </summary>
        /// <param name="key"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object Get(string key, Type type)
        {
            var data = this.Get(key);
            if (data == null)
            {
                if (type.IsValueType)
                    return Activator.CreateInstance(type);
                return null;
            }

            return this.binarySerializable.Deserialize(type, data);
        }

        /// <summary>
        /// 按定义的序列化方法设置一个对象缓存
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expires"></param>
        public void Set(string key, object value, int expires)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            byte[] data = this.binarySerializable.Serialize(value);
            this.Set(key, data, expires, expires);
        }

        /// <summary>
        /// 按定义的序列化方法设置一个对象缓存
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="existsExpires"></param>
        /// <param name="initExpires"></param>
        public void Set(string key, object value, int initExpires, int existsExpires)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            byte[] data = this.binarySerializable.Serialize(value);
            this.Set(key, data, initExpires, existsExpires);
        }

        void ICache.Delete(string key)
        {
            this.Delete(key);
        }

        string ICache.Get(string key)
        {
            return this.GetString(key);
        }
    }
}