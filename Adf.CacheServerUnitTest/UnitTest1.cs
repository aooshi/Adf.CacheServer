using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Adf.CacheServerUnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Console.WriteLine("initialize 10000 cache item");

            var cacheItems = new List<string>(10000);

            for (int i = 0; i < cacheItems.Capacity; i++)
            {
                var key = "k" + i;
                var value = "value" + key;
                cacheItems.Add(key);

                Adf.CacheServerClient.Instance.Set(key, value, 10000);
            }

            var counter = new Adf.Counter( cacheItems.Capacity );
            
            for (int i = 0; i < cacheItems.Capacity; i++)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(this.CheckItem,new object[]{ i, counter });
            }

            var handle = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.ManualReset);

            System.Threading.ThreadPool.QueueUserWorkItem(obj => {

                while (true)
                {
                    System.Threading.Thread.Sleep(1000);
                    if (counter.Value == 0)
                    {
                        handle.Set();
                    }
                }
            });

            handle.WaitOne();

            Console.WriteLine("TestMethod1 Completed");
            
        }

        private void CheckItem(object state)
        {
            var arr = (object[])state;
            var i = (int)arr[0];
            var counter = (Adf.Counter)arr[1];

            //
            var key = "k" + i;
            var value = "value" + key;

            //
            var v = Adf.CacheServerClient.Instance.GetString(key);
            if (v == null)
            {
                System.Diagnostics.Debug.WriteLineIf(value == v, "index: " + i + ", value: " + value + ", cache: " + v);
            }

            //
            var deleted = Adf.CacheServerClient.Instance.Delete(key);
            System.Diagnostics.Debug.WriteLineIf(deleted == false, "delete failure: " + key);

            //
            counter.Decrement();
            
        }
    }
}
