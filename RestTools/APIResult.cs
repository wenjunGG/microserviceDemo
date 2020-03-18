using System;
using System.Collections.Generic;
using System.Text;

namespace RestTools
{
    public class APIResult<T>
    {
        public int Code { get; set; }
        public T Data { get; set; }
        public String Message { get; set; }
    }
}
