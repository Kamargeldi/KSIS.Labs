using System;
using System.Net;


namespace TraceRouteUtil
{
    class Program
    {
        static void Main(string[] args)
        {
            short ttl = 1;
            bool reply = false;
            while (reply == false)
            { 
                Ping ping = new Ping();
                reply = ping.PingRequest("1.1.1.1", ttl, 3);
                Console.WriteLine();
                ttl++;
            }

            Console.ReadKey();
        }
    }
}
