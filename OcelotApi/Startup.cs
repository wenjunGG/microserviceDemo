using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Ocelot.Cache.Middleware;
using Ocelot.DependencyInjection;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.DownstreamUrlCreator.Middleware;
using Ocelot.Errors.Middleware;
using Ocelot.Headers.Middleware;
using Ocelot.LoadBalancer.Middleware;
using Ocelot.Middleware;
using Ocelot.Middleware.Pipeline;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Polly;
using Ocelot.Request.Middleware;
using Ocelot.Requester.Middleware;
using Ocelot.RequestId.Middleware;
using Ocelot.Responder.Middleware;

namespace OcelotApi
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
            services.AddOcelot(Configuration).AddConsul().AddConfigStoredInConsul().AddPolly();

            //配置跨域
            //配置跨域处理
            services.AddCors(options =>
            {
                options.AddPolicy("any", builder =>
                {
                    builder.AllowAnyOrigin() //允许任何来源的主机访问
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                       // .AllowCredentials();//指定处理cookie
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //设置跨域
            app.UseCors("any");

            var configuration = new OcelotPipelineConfiguration
            {
                //PreErrorResponderMiddleware = async (ctx, next) =>
                //{
                //    //String token = ctx.HttpContext.Request.Headers["token"].FirstOrDefault();//这里可以进行接收的客户端token解析转发
                //    //ctx.HttpContext.Request.Headers.Add("X-Hello", "666");
                //    //await next.Invoke();
                //}

                

            PreErrorResponderMiddleware = async (ctx, next) =>
                 {
                     if (!ctx.HttpContext.Request.Path.Value.StartsWith("/auth"))//不以auth开头的一律校验
                     {
                         String token = ctx.HttpContext.Request.Headers["Authentication"].FirstOrDefault();
                         if (string.IsNullOrWhiteSpace(token))
                         {
                             ctx.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                             using (StreamWriter writer = new StreamWriter(ctx.HttpContext.Response.Body))
                             {
                                 writer.Write("token required");
                             }
                         }
                         else
                         {
                             var secret = "GQDstcKsx0NHjPOuXOYg5MbeJ1XT0uFiwDVvVBrk";
                             try
                             {
                                 IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
                                 IJsonSerializer serializer = new JsonNetSerializer();
                                 IDateTimeProvider provider = new UtcDateTimeProvider();
                                 IJwtValidator validator = new JwtValidator(serializer, provider);
                                 IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                                 IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder, algorithm);
                                 var json = decoder.Decode(token, secret, verify: true);
                                 Console.WriteLine(json);
                                 dynamic payload = JsonConvert.DeserializeObject<dynamic>(json);
                                 string userName = payload.UserName;
                                 ctx.HttpContext.Request.Headers.Add("X-UserName", userName);//将解析出来的用户名传输给后端服务器。
                             }
                             catch (TokenExpiredException)
                             {
                                 ctx.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                 using (StreamWriter writer = new StreamWriter(ctx.HttpContext.Response.Body))
                                 {
                                     writer.Write("Token has expired");
                                 }
                             }
                             catch (SignatureVerificationException)
                             {
                                 ctx.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                 using (StreamWriter writer = new StreamWriter(ctx.HttpContext.Response.Body))
                                 {
                                     writer.Write("Token has invalid signature");
                                 }
                             }
                         }
                     }
                     await next.Invoke();
                 }
            };
            app.UseOcelot(configuration).Wait();
        }
    }
}
