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

            ////ҵ���߼������ڳ��������ռ�
            //Assembly service = Assembly.Load("NetCoreDemo.Service");
            ////�ӿڲ����ڳ��������ռ�
            //Assembly repository = Assembly.Load("NetCoreDemo.Repository");
            ////�Զ�ע��
            //builder.RegisterAssemblyTypes(service, repository)
            //    .Where(t => t.Name.EndsWith("Service"))
            //    .AsImplementedInterfaces();

            ////ע��ִ�������IRepository�ӿڵ�Repository��ӳ��
            //builder.RegisterGeneric(typeof(Repository<>))
            //    //InstancePerDependency��Ĭ��ģʽ��ÿ�ε��ã���������ʵ��������ÿ�����󶼴���һ���µĶ���
            //    .As(typeof(IRepository<>)).InstancePerDependency();

           RegisterServices(this.GetType().Assembly, builder);
        }

        /// <summary>
        /// ������������ע��
        /// </summary>
        private static void RegisterServices(Assembly assembly, ContainerBuilder services)
        {
            //���������е�����public����
            foreach (Type type in assembly.GetExportedTypes())
            {
                //�ж������Ƿ��б�ע��CustomInterceptorAttribute�ķ���
                bool hasHystrixCommandAttr = type.GetMethods().Any(m => m.GetCustomAttribute(typeof(HystrixCommandAttribute)) != null);
                if (hasHystrixCommandAttr)
                {
                    services.RegisterType(type);
                }
            }
        }
    }
   
}
