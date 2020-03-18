using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Mvc;
using RestTools;

namespace JWTTokenServer1.Controllers
{
    [Route("api/[Controller]")]
    public class AuthController : Controller
    {
        [HttpGet]
        [Route(nameof(RequestToken))]
        public APIResult<string> RequestToken(string userName, string password)
        {
            APIResult<string> result = new APIResult<string>();
            if (userName == "lwj" && password == "123")//todo:连数据库
            {
                var payload = new Dictionary<string, object>
            {
                { "UserName", userName },
                { "UserId", 666 }
            };
                var secret = "GQDstcKsx0NHjPOuXOYg5MbeJ1XT0uFiwDVvVBrk";//不要泄露
                IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
                IJsonSerializer serializer = new JsonNetSerializer();
                IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                IJwtEncoder encoder = new JwtEncoder(algorithm, serializer, urlEncoder);
                var token = encoder.Encode(payload, secret);
                result.Code = 0;
                result.Data = token;
            }
            else
            {
                result.Code = -1;
                result.Message = "username or password error";
            }
            return result;
        }
    }
}