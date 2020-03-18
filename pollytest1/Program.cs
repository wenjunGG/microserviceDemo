using Polly;
using Polly.Caching;
using Polly.Timeout;
using System;
using System.Threading;

namespace pollytest1
{
    class Program
    {
        static void Main(string[] args)
        {

            // policy  Fallback 回调
            //try
            //{
            //    ISyncPolicy policy = Policy.Handle<ArgumentException>(ex => ex.Message == "年龄参数错误"
            //       )
            //       // .RetryForever(); //一直重试
            //        .Fallback(() =>
            //        {
            //            Console.WriteLine("出错了");
            //        });

            //    policy.Execute(() => {
            //        //这里是可能会产生问题的业务系统代码
            //        Console.WriteLine("开始任务");
            //        throw new ArgumentException("年龄参数错误");
            //        //throw new Exception("haha");
            //        //Console.WriteLine("完成任务");
            //    });
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"未处理异常:{ex}");
            //}


            ////连续出错6次之后熔断5秒(不会再去尝试执行业务代码）
            //ISyncPolicy policy = Policy.Handle<Exception>()
            //.CircuitBreaker(6, TimeSpan.FromSeconds(5));//连续出错6次之后熔断5秒(不会再去尝试执行业务代码）。
            //while (true)
            //{
            //    Console.WriteLine("开始Execute");
            //    try
            //    {
            //        policy.Execute(() =>
            //        {
            //            Console.WriteLine("开始任务");
            //            throw new Exception("出错");
            //            Console.WriteLine("完成任务");
            //        });
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine("execute出错" + ex.GetType() + ":" + ex.Message);
            //    }
            //    Thread.Sleep(500);
            //}


            //如果发生超时，重试最多3次（也就是说一共执行4次哦）。
            //ISyncPolicy policy = Policy.Handle<TimeoutRejectedException>()
            //.Retry(1);
            //policy = policy.Wrap(Policy.Timeout(3, TimeoutStrategy.Pessimistic));
            //policy.Execute(() =>
            //{
            //    Console.WriteLine("开始任务");
            //    Thread.Sleep(5000);
            //    Console.WriteLine("完成任务");
            //});

            //目前只支持Polly 5.9.0，不支持最新版 缓存的意思就是N秒内只调用一次方法，其他的调用都返回缓存的数据。
            //Install-Package Microsoft.Extensions.Caching.Memory
            Microsoft.Extensions.Caching.Memory.IMemoryCache memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            //Install-Package Polly.Caching.MemoryCache
            Polly.Caching.MemoryCache.MemoryCacheProvider memoryCacheProvider = new Polly.Caching.MemoryCache.MemoryCacheProvider(memoryCache);

            CachePolicy policy = Policy.Cache(memoryCacheProvider, TimeSpan.FromSeconds(5));
            Random rand = new Random();
            while (true)
            {
                int i = rand.Next(5);
                Console.WriteLine("产生" + i);
                var context = new Context("doublecache" + i);
                int result = policy.Execute(ctx =>
                {
                    Console.WriteLine("Execute计算" + i);
                    return i * 2;
                }, context);
                Console.WriteLine("计算结果：" + result);
                Thread.Sleep(500);
            }
        }
    }
}
