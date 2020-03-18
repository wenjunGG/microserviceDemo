using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RestEntity;

namespace WebApplicationDemo1.Controllers
{
    [Route("api/Home")]
    public class HomeController : Controller
    {
        public IActionResult Get()
        {

            TestUserInfo testUser = new TestUserInfo();
            testUser.Id = Guid.NewGuid().ToString();
            testUser.UserName = "Lewis";
            testUser.UserAge = 26;

            return Json(testUser);
        }
    }
}