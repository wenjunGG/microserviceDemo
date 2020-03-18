using AspectCore.DynamicProxy;
using System;

namespace aoptest1
{
    class Program
    {
        static void Main(string[] args)
        {
            ProxyGeneratorBuilder proxyGeneratorBuilder = new ProxyGeneratorBuilder();
            using (IProxyGenerator proxyGenerator = proxyGeneratorBuilder.Build())
            {
                Person p = proxyGenerator.CreateClassProxy<Person>();
                p.Say("Hello World");
                Console.WriteLine(p.GetType());
                Console.WriteLine(p.GetType().BaseType);
            }
            Console.ReadKey();
        }
    }
}
