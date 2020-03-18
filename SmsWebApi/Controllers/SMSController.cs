using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SmsWebApi.Controllers
{
    [Route("api/[Controller]")]
    public class SMSController : Controller
    {
        [Route("Send")]
        public bool Send(string msg)
        {
            Console.WriteLine("发送短信" + msg);
            return true;
        }
    }
}