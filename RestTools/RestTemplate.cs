using Consul;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace RestTools
{
    public class RestTemplate
    {
        private String consulServerUrl;

        public RestTemplate(String consulServerUrl)
        {
            this.consulServerUrl = consulServerUrl;
        }

        /// <summary>
        /// 获取服务的一个IP地址
        /// </summary>
        /// <param name="serviceName">consul服务IP</param>
        /// <returns></returns>
        private async Task<String> ResolveRootUrlAsync(String serviceName)
        {
            using (var consulClient = new ConsulClient(c => c.Address = new Uri(consulServerUrl)))
            {
                var services = (await consulClient.Agent.Services()).Response;
                var agentServices = services.Where(s => s.Value.Service.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase)).Select(s => s.Value);
                //TODO:注入负载均衡策略
                var agentService = agentServices.ElementAt(Environment.TickCount % agentServices.Count());
                //根据当前TickCount对服务器个数取模，“随机”取一个机器出来，避免“轮询”的负载均衡策略需要计数加锁问题
                return agentService.Address + ":" + agentService.Port;
            }
        }

        /// <summary>
        /// //把http://apiservice1/api/values转换为http://192.168.1.1:5000/api/values
        /// </summary>
        private async Task<String> ResolveUrlAsync(String url)
        {
            Uri uri = new Uri(url);
            String serviceName = uri.Host;//apiservice1
            String realRootUrl = await ResolveRootUrlAsync(serviceName);
            return uri.Scheme + "://" + realRootUrl + uri.PathAndQuery;
        }

        /// <summary>
        /// Get请求转换
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url">请求地址</param>
        /// <param name="requestHeaders">请求头</param>
        /// <returns></returns>
        public async Task<ResponseEntity<T>> GetForEntityAsync<T>(String url, HttpRequestHeaders requestHeaders = null)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                HttpRequestMessage requestMsg = new HttpRequestMessage();
                if (requestHeaders != null)
                {
                    foreach (var header in requestHeaders)
                    {
                        httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
                requestMsg.Method = HttpMethod.Get;
                //http://apiservice1/api/values转换为http://192.168.1.1:5000/api/values
                requestMsg.RequestUri = new Uri(await ResolveUrlAsync(url));

                var result = await httpClient.SendAsync(requestMsg);
                ResponseEntity<T> responseEntity = new ResponseEntity<T>();
                responseEntity.StatusCode = result.StatusCode;
                String bodyStr = await result.Content.ReadAsStringAsync();
                responseEntity.Body = JsonConvert.DeserializeObject<T>(bodyStr);
                responseEntity.Headers = responseEntity.Headers;
                return responseEntity;
            }
        }
    }
}
