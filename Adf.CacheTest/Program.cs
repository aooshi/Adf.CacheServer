using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace Adf.CacheTest
{
    class Program
    {
        static void Main(string[] args)
        {
            const int threadCount = 10;


            Console.WriteLine("initialize 10000 cache item");

            var cacheItems = new List<string>(10000);

            for (int i = 0; i < cacheItems.Capacity; i++)
            {
                var key = "k" + i;
                var value = "value" + key;
                cacheItems.Add(key);
                Adf.CacheServerClient.Instance.Set(key, value, 1000);
            }

            bool run = true;




            //Console.WriteLine("rand " + threadCount + " thread set cache");
            //for (int i = 0; i < threadCount; i++)
            //{
            //    new Thread(() =>
            //    {
            //        var r = new Random();
            //        var n = 0;
            //        var k = "";
            //        var total = 0L;
            //        var stopwatch = new Stopwatch();
            //        stopwatch.Start();
            //        while (run)
            //        {
            //            n = r.Next(0, cacheItems.Capacity - 1);
            //            k = "k" + n;
            //            if (n % 2 == 0)
            //            {
            //                Adf.CacheServerClient.Instance.Set(k, DateTime.Now.ToString(), 10);
            //            }
            //            else if (n % 5 == 0)
            //            {
            //                Adf.CacheServerClient.Instance.Set(k, n, 10);
            //            }
            //            else
            //            {
            //                Adf.CacheServerClient.Instance.Set(k, i.ToString(), 10);
            //            }
            //            total++;
            //        }
            //        stopwatch.Stop();

            //        Console.WriteLine("set {3}-> total:{0}, seconds:{1}, {2} set/s"
            //            , total
            //            , (double)(stopwatch.ElapsedMilliseconds / 1000)
            //            , total / (double)(stopwatch.ElapsedMilliseconds / 1000)
            //            , i
            //            );

            //    }).Start();
            //}

            Console.WriteLine("rand " + threadCount + " thread get cache");
            for (int i = 0; i < threadCount; i++)
            {
                new Thread(() =>
                {
                    var r = new Random();
                    var n = "";
                    var n2 = "";
                    var k = "";
                    var total = 0L;
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    while (run)
                    {
                        n = r.Next(0, cacheItems.Capacity - 1).ToString();
                        k = "k" + n;
                        Adf.CacheServerClient.Instance.Set(k, n, 1000);
                        n2 = Adf.CacheServerClient.Instance.GetString(k);
                        if (n2 != n)
                        {
                            Console.WriteLine("{0} <> {1}", n, n2);
                        }
                        
                        total++;
                    }
                    stopwatch.Stop();

                    Console.WriteLine("get {3}-> total:{0}, seconds:{1}, {2} get/s"
                        , total
                        , (double)(stopwatch.ElapsedMilliseconds / 1000)
                        , total / (double)(stopwatch.ElapsedMilliseconds / 1000)
                        , i
                        );

                }).Start();
            }



            Console.WriteLine("any key stop");
            Console.ReadLine();
            run = false;

            Console.ReadLine();
        }
    }
}