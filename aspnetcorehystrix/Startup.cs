using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AspectCore.Extensions.Autofac;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace aspnetcorehystrix
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            //services.AddMvc();
            //RegisterServices(this.GetType().Assembly, services);
            //return services.BuildServiceContextProvider();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

          
        }


        public void ConfigureContainer(ContainerBuilder builder)
        {

            ////业务逻辑层所在程序集命名空间
            //Assembly service = Assembly.Load("NetCoreDemo.Service");
            ////接口层所在程序集命名空间
            //Assembly repository = Assembly.Load("NetCoreDemo.Repository");
            ////自动注入
            //builder.RegisterAssemblyTypes(service, repository)
            //    .Where(t => t.Name.EndsWith("Service"))
            //    .AsImplementedInterfaces();

            ////注册仓储，所有IRepository接口到Repository的映射
            //builder.RegisterGeneric(typeof(Repository<>))
            //    //InstancePerDependency：默认模式，每次调用，都会重新实例化对象；每次请求都创建一个新的对象；
            //    .As(typeof(IRepository<>)).InstancePerDependency();

           RegisterServices(this.GetType().Assembly, builder);
        }

        /// <summary>
        /// 根据特性批量注入
        /// </summary>
        private static void RegisterServices(Assembly assembly, ContainerBuilder services)
        {
            //遍历程序集中的所有public类型
            foreach (Type type in assembly.GetExportedTypes())
            {
                //判断类中是否有标注了CustomInterceptorAttribute的方法
                bool hasHystrixCommandAttr = type.GetMethods().Any(m => m.GetCustomAttribute(typeof(HystrixCommandAttribute)) != null);
                if (hasHystrixCommandAttr)
                {
                    services.RegisterType(type);
                }
            }
        }
    }
   
}
