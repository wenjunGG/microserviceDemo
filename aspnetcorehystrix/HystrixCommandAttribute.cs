﻿using AspectCore.DynamicProxy;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace aspnetcorehystrix
{
    /// <summary>
    /// 熔断框架
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class HystrixCommandAttribute : AbstractInterceptorAttribute
    {
        #region 属性
        /// <summary>
        /// 最多重试几次，如果为0则不重试
        /// </summary>
        public int MaxRetryTimes { get; set; } = 0;

        /// <summary>
        /// 重试间隔的毫秒数
        /// </summary>
        public int RetryIntervalMilliseconds { get; set; } = 100;

        /// <summary>
        /// 是否启用熔断
        /// </summary>
        public bool EnableCircuitBreater { get; set; } = true;

        /// <summary>
        /// 熔断前出现允许错误几次
        /// </summary>
        public int ExceptionAllowedBeforeBreaking { get; set; } = 3;

        /// <summary>
        /// 熔断多长时间（毫秒 ）
        /// </summary>
        public int MillisecondsOfBreak { get; set; } = 1000;

        /// <summary>
        /// 执行超过多少毫秒则认为超时（0表示不检测超时）
        /// </summary>
        public int TimeOutMilliseconds { get; set; } = 0;

        /// <summary>
        /// 缓存多少毫秒（0表示不缓存），用“类名+方法名+所有参数ToString拼接”做缓存Key
        /// </summary>
        public int CacheTTLMilliseconds { get; set; } = 2000;

        private Policy policy;

        //缓存
        private static readonly Microsoft.Extensions.Caching.Memory.IMemoryCache memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());

        /// <summary>
        /// 降级方法名
        /// </summary>
        public string FallBackMethod { get; set; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 熔断框架
        /// </summary>
        /// <param name="fallBackMethod">降级方法名</param>
        public HystrixCommandAttribute(string fallBackMethod)
        {
            this.FallBackMethod = fallBackMethod;
        }
        #endregion



        public override async Task Invoke(AspectContext context, AspectDelegate next)
        {
            //一个HystrixCommand中保持一个policy对象即可
            //其实主要是CircuitBreaker要求对于同一段代码要共享一个policy对象
            //根据反射原理，同一个方法就对应一个HystrixCommandAttribute，无论几次调用，
            //而不同方法对应不同的HystrixCommandAttribute对象，天然的一个policy对象共享
            //因为同一个方法共享一个policy，因此这个CircuitBreaker是针对所有请求的。
            //Attribute也不会在运行时再去改变属性的值，共享同一个policy对象也没问题
            
            lock (this)
            {
                if (policy == null)
                {
                    policy = Policy.Handle<Exception>()
                        .FallbackAsync(async (ctx, t) =>
                        {
                            AspectContext aspectContext = (AspectContext)ctx["aspectContext"];
                            var fallBackMethod = context.ServiceMethod.DeclaringType.GetMethod(this.FallBackMethod);
                            Object fallBackResult = fallBackMethod.Invoke(context.Implementation, context.Parameters);
                        //不能如下这样，因为这是闭包相关，如果这样写第二次调用Invoke的时候context指向的
                        //还是第一次的对象，所以要通过Polly的上下文来传递AspectContext
                        //context.ReturnValue = fallBackResult;
                        aspectContext.ReturnValue = fallBackResult;

                        }, async (ex, t) => { });

                    if (MaxRetryTimes > 0)//重试
                    {
                        policy = policy.WrapAsync(Policy.Handle<Exception>().WaitAndRetryAsync(MaxRetryTimes, i => TimeSpan.FromMilliseconds(RetryIntervalMilliseconds)));

                    }

                    if (EnableCircuitBreater)//熔断
                    {
                        policy = policy.WrapAsync(Policy.Handle<Exception>().CircuitBreakerAsync(ExceptionAllowedBeforeBreaking, TimeSpan.FromMilliseconds(MillisecondsOfBreak)));
                    }

                    if (TimeOutMilliseconds > 0)//超时
                    {
                        policy = policy.WrapAsync(Policy.TimeoutAsync(() => TimeSpan.FromMilliseconds(TimeOutMilliseconds), Polly.Timeout.TimeoutStrategy.Pessimistic));
                    }

                }
            }

            //把本地调用的AspectContext传递给Polly,主要给FallBackMethod中使用，避免闭包的坑
            Context pollyCtx = new Context();
            pollyCtx["aspectContext"] = context;

            if (CacheTTLMilliseconds > 0)
            {
                //用类名+方法名+参数的下划线连接起来作为缓存key
                string cacheKey = "HystrixMethodCacheManager_Key_" + context.ServiceMethod.DeclaringType + "." + context.ServiceMethod + string.Join("_", context.Parameters);

                //尝试去缓存中获取。如果找到了，则直接用缓存中的值做返回值
                if (memoryCache.TryGetValue(cacheKey, out var cacheValue))
                {
                    context.ReturnValue = cacheValue;
                }
                else
                {
                    //如果缓存中没有，则执行实际被拦截的方法
                    await policy.ExecuteAsync(ctx => next(context), pollyCtx);
                    //存入缓存中
                    using (var cacheEntry = memoryCache.CreateEntry(cacheKey))
                    {
                        cacheEntry.Value = context.ReturnValue;
                        cacheEntry.AbsoluteExpiration = DateTime.Now + TimeSpan.FromMilliseconds(CacheTTLMilliseconds);
                    }
                }
            }
            else//如果没有启用缓存，就直接执行业务方法
            {
                await policy.ExecuteAsync(ctx => next(context), pollyCtx);
            }
        }
    }
}
