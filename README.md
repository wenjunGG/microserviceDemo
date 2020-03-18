# 了解微服务



微服务架构风格是一种使用一套小服务来开发单个应用的方式途径，每个服务运行在自己的进程中，并使用轻量级机制通信，通常是HTTP API，这些服务基于业务能力构建，并能够通过自动化部署机制来独立部署，这些服务使用不同的编程语言实现，以及不同数据存储技术，并保持最低限度的集中式管理。

## 开发环境
vs2019
Install-Package Consul
Install-Package Ocelot 

### 初步认识 Consul
Consul是HashiCorp公司推出的开源工具，Consul是分布式的、高可用的、 可横向扩展的用于实现分布式系统的服务发现与配置。

Consul官网：https://www.consul.io/
服务发现：Consul提供了通过DNS或者HTTP接口的方式来注册服务和发现服务。一些外部的服务通过Consul很容易的找到它所依赖的服务。
健康检测：Consul的Client提供了健康检查的机制，可以通过用来避免流量被转发到有故障的服务上。
Key/Value存储：应用程序可以根据自己的需要使用Consul提供的Key/Value存储。 Consul提供了简单易用的HTTP接口，结合其他工具可以实现动态配置、功能标记、领袖选举等等功能。
多数据中心：Consul支持开箱即用的多数据中心. 这意味着用户不需要担心需要建立额外的抽象层让业务扩展到多个区域。

```javascript
String ip = Configuration["ip"];//部署到不同服务器的时候不能写成127.0.0.1或者0.0.0.0,因为这是让服务消费者调用的地址
            Int32 port = Int32.Parse(Configuration["port"]);
            //向consul注册服务
            ConsulClient client = new ConsulClient(config => 
			config.Address = new Uri("http://127.0.0.1:8500"));
            Task<WriteResult> result = client.Agent.ServiceRegister(
			new AgentServiceRegistration()
            {
                ID = "sms2" + Guid.NewGuid(),//服务编号，不能重复，用Guid最简单
                Name = "sms2",//服务的名字
                Address = ip,
				//我的ip地址(可以被其他应用访问的地址，本地测试可以用 127.0.0.1，机房环境中一定要写自己的内网ip地址)
                Port = port,//我的端口
                Check = new AgentServiceCheck()
                {
				DeregisterCriticalServiceAfter=TimeSpan.FromSeconds(5),
					//服务停止多久后反注册
                    Interval = TimeSpan.FromSeconds(10),//健康检查时间间隔，或者称为心跳间隔
                    HTTP = $"http://{ip}:{port}/api/health",//健康检查地址,
                    Timeout = TimeSpan.FromSeconds(5)
                }
            });
```
将服务注册到consul

### 初步认识 Ocelot
Ocelot 是一个用. NET Core 实现并且开源的 API 网关, 它功能强大, 包括了: 路由请求聚合服务发现认证鉴权限流熔断并内置了负载均衡器与 Service FabricButterfly Tracing 集成这些功能只都只需要简单的配置即可完成

 Ocelot 是一堆的 asp.net core middleware 组成的一个管道当它拿到请求之后会用一个 request builder 来构造一个 HttpRequestMessage 发到下游的真实服务器, 等下游的服务返回 response 之后再由一个 middleware 将它返回的 HttpResponseMessage 映射到 HttpResponse 上

API 网关 它是系统的暴露在外部的一个访问入口这个有点像代理访问的家伙, 就像一个公司的门卫承担着寻址限制进入安全检查位置引导等等功能

##### ocelot 配置
```javascript
{
  "GlobalConfiguration": {
    "BaseUrl": "http://127.0.0.1:5000",
    "ServiceDiscoveryProvider": {
      "Host": "127.0.0.1", // Consul Service IP
      "Port": 8500, // Consul Service Port    
      "Type": "Consul"
    }
  },
  "ReRoutes": [
    {
      /**发送短信  通过名称 找服务似乎找不到 **/
      "DownstreamPathTemplate": "/api/sms/{url}",
      "DownstreamScheme": "http",
      "UpstreamPathTemplate": "/sms/{url}",
      "UpstreamHttpMethod": [ "Get" ],
      "ServiceName": "sms2",
      "LoadBalancerOptions": "RoundRobin",
      "UseServiceDiscovery": true,
      "ReRoutesCaseSensitive": false, // non case sensitive    
      "RateLimitOptions": {
        "ClientWhitelist": [], //不受限制的白名单
        "EnableRateLimiting": true, //启用限流
        "Period": "30s", //统计时间段：1s、1m、1h、1d
        "PeriodTimespan": 10, //一旦碰到一次“超限”，多少秒后重新记数可以重新请求。
        "Limit": 5 //指定时间段内最多请求次数
      }
    },
    /**发送email**/
    {
      "DownstreamPathTemplate": "/api/email/{url}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "127.0.0.1",
          "Port": 5010
        }
      ],
      "UpstreamPathTemplate": "/youjian/{url}",
      "UpstreamHttpMethod": [ "Get", "Post" ]
    },
    /**auth**/
    {
      "DownstreamPathTemplate": "/api/Auth/{url}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "127.0.0.1",
          "Port": 5001
        }
      ],
      "UpstreamPathTemplate": "/Auth/{url}",
      "UpstreamHttpMethod": [ "Get", "Post" ],
      "QoSOptions": {
        "ExceptionsAllowedBeforeBreaking": 3,
        "DurationOfBreak": 10,
        "TimeoutValue": 5000
      }
    }
  ]
   
}

```

##### ocelot 的使用 以及认证

``` javascript
 var configuration = new OcelotPipelineConfiguration
            {

            PreErrorResponderMiddleware = async (ctx, next) =>
                 {
                  	if(!ctx.HttpContext.Request.Path.Value.StartsWith("/auth"))//不以auth开头的一律校验
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
```

### 链接 Links
[Asp.Net Core微服务初体验](https://www.cnblogs.com/wyt007/p/9150116.html)

