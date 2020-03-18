using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspectCore.DynamicProxy;
using Microsoft.AspNetCore.Mvc;

namespace aspnetcorehystrix.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ProxyGeneratorBuilder proxyGeneratorBuilder = new ProxyGeneratorBuilder();
            using (IProxyGenerator proxyGenerator = proxyGeneratorBuilder.Build())
            {
                
                Person p = proxyGenerator.CreateClassProxy<Person>();
                Console.WriteLine(p.HelloAsync("Hello World").Result);
                Console.WriteLine(p.Add(1, 2));
            }
            return Ok("ok");
        }
    }
}