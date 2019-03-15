using System;
using System.Collections.Generic;
using System.Text;

namespace Adf.CacheServer
{
    /// <summary>
    /// Cache Server Class
    /// </summary>
    public class CacheServer
    {
        public bool Delete(string key)
        {
            if (key == null)
            {
                return false;
            }

            var hashCode = HashHelper.GetHashCode(key);
            var manager = Program.CacheManagers[hashCode % Program.HASH_POOL_SIZE];
            var result = false;
            lock (manager.SyncObject)
            {
                result = manager.Remove(key);
            }
            return result;
        }
        
        public byte[] Get(string key)
        {
            if (key == null)
            {
                return null;
            }

            var hashCode = HashHelper.GetHashCode(key);
            var manager = Program.CacheManagers[hashCode % Program.HASH_POOL_SIZE];
            byte[] result = null;
            lock (manager.SyncObject)
            {
                result = manager.Get(key);
            }
            return result;
        }

        public int Set(string key, byte[] value, int initExpires, int existsExpires)
        {
            if (key == null)
            {
                return 0;
            }

            var hashCode = HashHelper.GetHashCode(key);
            var manager = Program.CacheManagers[hashCode % Program.HASH_POOL_SIZE];
            var result = 0;
            lock (manager.SyncObject)
            {
                result = manager.Set(key, value, initExpires, existsExpires);
            }
            return result;
        }

        public bool Add(string key, byte[] value, int expires)
        {
            if (key == null)
            {
                return false;
            }

            var hashCode = HashHelper.GetHashCode(key);
            var manager = Program.CacheManagers[hashCode % Program.HASH_POOL_SIZE];
            var result = false;
            lock (manager.SyncObject)
            {
                result = manager.Add(key, value, expires);
            }
            return result;
        }

        public long Increment(string key, long num, long initValue, int initExpires, int existsExpires)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            var hashCode = HashHelper.GetHashCode(key);
            var manager = Program.CacheManagers[hashCode % Program.HASH_POOL_SIZE];
            var result = 0L;
            lock (manager.SyncObject)
            {
                var data = manager.Get(key);
                if (data != null && data.Length == 8)
                {
                    //incr
                    result = BitConverter.ToInt64(data, 0);
                    result += num;
                }
                else
                {
                    //new
                    result = initValue;
                }
                data = BitConverter.GetBytes(result);
                manager.Set(key, data, initExpires, existsExpires);
            }
            return result;
        }

        public bool IncrementEx(string key, long num, int existsExpires, out long newNum)
        {
            newNum = 0;
            if (key == null)
            {
                return false;
            }

            var hashCode = HashHelper.GetHashCode(key);
            var manager = Program.CacheManagers[hashCode % Program.HASH_POOL_SIZE];
            var result = false;
            lock (manager.SyncObject)
            {
                var data = manager.Get(key);
                if (data != null && data.Length == 8)
                {
                    //incr
                    newNum = BitConverter.ToInt64(data, 0);
                    newNum += num;
                    data = BitConverter.GetBytes(newNum);
                    manager.Set(key, data, existsExpires, existsExpires);
                    result = true;
                }
            }
            return result;
        }
    }
}