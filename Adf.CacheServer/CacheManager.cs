using System;
using System.Collections.Generic;
using System.Text;
using Adf;

namespace Adf.CacheServer
{
    /// <summary>
    /// 缓存管理器
    /// </summary>
    internal class CacheManager
    {
        private const int SIZE = 10000;
        private int maxResetCapacity = 128 * 10000;
        //
        private int[] buckets = new int[SIZE];
        //slots
        private int[] hashCodes = new int[SIZE];
        private int[] nexts = new int[SIZE];
        private string[] keys = new string[SIZE];
        private int[] expires = new int[SIZE];
        private byte[][] values = new byte[SIZE][];
        //
        private int size = SIZE;
        private int count = 0;
        private int lastIndex = 0;
        private int freeIndex = -1;
        //
        public readonly object SyncObject = new object();

        //
        public int Count
        {
            get { return this.count; }
        }

        //
        public CacheManager()
        {
            this.maxResetCapacity = Adf.ConfigHelper.GetSettingAsInt("MaxResetCapacity", this.maxResetCapacity);
            if (this.maxResetCapacity < 10000)
            {
                throw new Adf.ConfigException("MaxResetCapacity configuration invalid, limit 10000+ ");
            }
        }

        //public bool Contains(string key)
        //{
        //    int hashCode = HashHelper.GetHashCode(key);
        //    int bucket = hashCode % this.length;
        //    //
        //    for (int i = this.buckets[bucket] - 1; i >= 0; i = this.nexts[i])
        //    {
        //        if (this.hashCodes[i] == hashCode && this.keys[i] == key)
        //        {
        //            //return true;
        //            return this.expires[i] > (uint)Environment.TickCount / 1000;
        //        }
        //    }
        //    //
        //    return false;
        //}

        public byte[] Get(string key)
        {
            int hashCode = HashHelper.GetHashCode(key);
            int bucket = hashCode % this.size;
            int nowTick = Environment.TickCount;
            //
            for (int i = this.buckets[bucket] - 1; i >= 0; i = this.nexts[i])
            {
                if (this.hashCodes[i] == hashCode && this.keys[i] == key)
                {
                    if (nowTick - this.expires[i] > -1)
                    {
                        return null;
                    }
                    else
                    {
                        return this.values[i];
                    }
                }
            }
            //
            return null;
        }

        public bool Remove(string key)
        {
            int hashCode = HashHelper.GetHashCode(key);
            int bucket = hashCode % this.size;
            int last = -1;
            for (int i = this.buckets[bucket] - 1; i >= 0; last = i, i = this.nexts[i])
            {
                if (this.hashCodes[i] == hashCode && this.keys[i] == key)
                {
                    if (last < 0)
                    {
                        this.buckets[bucket] = this.nexts[i] + 1;
                    }
                    else
                    {
                        this.nexts[last] = this.nexts[i];
                    }
                    this.hashCodes[i] = -1;
                    this.keys[i] = null;
                    this.values[i] = null;
                    this.expires[i] = 0;
                    this.nexts[i] = this.freeIndex;
                    //
                    this.count--;
                    if (this.count == 0)
                    {
                        this.lastIndex = 0;
                        this.freeIndex = -1;
                    }
                    else
                    {
                        this.freeIndex = i;
                    }
                    return true;
                }
            }
            //
            return false;
        }

        public bool Add(string key, byte[] data, int expires)
        {
            int nowTick = Environment.TickCount;
            int hashCode = HashHelper.GetHashCode(key);
            int bucket = hashCode % this.size;
            for (int i = this.buckets[bucket] - 1; i >= 0; i = this.nexts[i])
            {
                if (this.hashCodes[i] == hashCode && this.keys[i] == key)
                {
                    if (this.expires[i] != 0 && nowTick - this.expires[i] > -1)
                    {
                        //expires, update
                        this.values[i] = data;
                        this.expires[i] = nowTick + (expires * 1000);
                        return true;
                    }
                    else
                    {
                        //exists
                        return false;
                    }
                }
            }
            //
            int index;
            if (this.freeIndex >= 0)
            {
                index = this.freeIndex;
                this.freeIndex = this.nexts[index];
            }
            else
            {
                if (this.lastIndex == this.size)
                {
                    ResetCapacity();
                    bucket = hashCode % this.size;
                }
                index = this.lastIndex;
                this.lastIndex++;
            }
            //
            this.hashCodes[index] = hashCode;
            this.keys[index] = key;
            this.values[index] = data;
            this.nexts[index] = this.buckets[bucket] - 1;
            this.expires[index] = nowTick + (expires * 1000);
            //
            this.buckets[bucket] = index + 1;
            this.count++;
            //
            return true;
        }

        //return:  1  replace, 2 add
        public int Set(string key, byte[] data, int initExpires, int existsExpires)
        {
            int nowTick = Environment.TickCount;
            int hashCode = HashHelper.GetHashCode(key);
            int bucket = hashCode % this.size;
            for (int i = this.buckets[bucket] - 1; i >= 0; i = this.nexts[i])
            {
                if (this.hashCodes[i] == hashCode && this.keys[i] == key)
                {
                    //exists
                    this.values[i] = data;
                    if (existsExpires > 0)
                    {
                        this.expires[i] = nowTick + (existsExpires * 1000);
                    }
                    return 1;
                }
            }
            //
            int index;
            if (this.freeIndex >= 0)
            {
                index = this.freeIndex;
                this.freeIndex = this.nexts[index];
            }
            else
            {
                if (this.lastIndex == this.size)
                {
                    ResetCapacity();
                    bucket = hashCode % this.size;
                }
                index = this.lastIndex;
                this.lastIndex++;
            }
            //
            this.hashCodes[index] = hashCode;
            this.keys[index] = key;
            this.values[index] = data;
            this.nexts[index] = this.buckets[bucket] - 1;
            this.expires[index] = nowTick + (initExpires * 1000);
            //
            this.buckets[bucket] = index + 1;
            this.count++;
            //
            return 2;
        }

        private void ResetCapacity()
        {
            int newSize = 0;
            if (this.size / 2 > this.maxResetCapacity)
                newSize = this.size + this.maxResetCapacity;
            else
                newSize = this.size * 2;

            //            
            int[] newHashCodes = new int[newSize];
            int[] newNexts = new int[newSize];
            string[] newKeys = new string[newSize];
            int[] newExpires = new int[newSize];
            byte[][] newValues = new byte[newSize][];

            //
            Array.Copy(this.hashCodes, 0, newHashCodes, 0, this.lastIndex);
            Array.Copy(this.nexts, 0, newNexts, 0, this.lastIndex);
            Array.Copy(this.keys, 0, newKeys, 0, this.lastIndex);
            Array.Copy(this.expires, 0, newExpires, 0, this.lastIndex);
            Array.Copy(this.values, 0, newValues, 0, this.lastIndex);

            //
            int[] newBuckets = new int[newSize];
            int bucket = 0;
            for (int i = 0; i < this.lastIndex; i++)
            {
                bucket = newHashCodes[i] % newSize;
                newNexts[i] = newBuckets[bucket] - 1;
                newBuckets[bucket] = i + 1;
            }

            //
            this.buckets = newBuckets;
            //
            this.hashCodes = newHashCodes;
            this.nexts = newNexts;
            this.keys = newKeys;
            this.expires = newExpires;
            this.values = newValues;
            //
            this.size = newSize;
        }

        public int CleanExpired()
        {
            int nowTick = Environment.TickCount;
            int cleanCount = 0;
            for (int index = 0; index < this.size; index++)
            {
                if (this.expires[index] != 0 && nowTick - this.expires[index] > -1)
                {
                    lock (this.SyncObject)
                    {
                        if (this.expires[index] != 0 && nowTick - this.expires[index] > -1)
                        {
                            if (this.Remove(this.keys[index]))
                            {
                                cleanCount++;
                            }
                        }
                    }
                }
            }
            return cleanCount;
        }

        //允许脏读
        public CacheProperty GetProperty(string key)
        {
            var property = new CacheProperty();
            //
            int hashCode = HashHelper.GetHashCode(key);
            int bucket = hashCode % this.size;
            //
            byte[] data;
            for (int i = this.buckets[bucket] - 1; i >= 0; i = this.nexts[i])
            {
                if (this.hashCodes[i] == hashCode && this.keys[i] == key)
                {
                    data = this.values[i];
                    //find
                    property.Expires = this.expires[i];
                    property.Key = this.keys[i];
                    property.Size = data == null ? 0 : data.Length;
                }
            }
            //
            return property;
        }

        //允许脏读
        public CacheProperty[] GetLastProperty(int size)
        {
            byte[] data;
            var list = new List<CacheProperty>(size);
            for (int i = this.lastIndex; i >= 0; i--)
            {
                data = this.values[i];

                //
                list.Add(new CacheProperty()
                {
                    Expires = this.expires[i],
                    Key = this.keys[i],
                    Size = data == null ? 0 : data.Length
                });

                //
                if (list.Count >= size)
                {
                    break;
                }
            }
            return list.ToArray();
        }
    }
}