using System;
using System.Net;


namespace TraceRouteUtil
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.Write(">  ");
                var commands = Console.ReadLine().Split();
                if (commands.Length != 2)
                {
                    Console.WriteLine("Enter 2 arguments.");
                    continue;
                }

                if (commands[0] == "tracert")
                {
                    IPAddress address;
                    if (!IPAddress.TryParse(commands[1], out address))
                    {
                        try
                        {
                            address = Dns.GetHostEntry(commands[1]).AddressList[0];
                        }
                        catch
                        {
                            Console.WriteLine("Invalid hostname.");
                            continue;
                        }
                    }
                    short ttl = 1;
                    bool reply = false;
                    while (!reply)
                    {
                        Ping ping = new Ping();
                        reply = ping.PingRequest(address, ttl, 3);
                        Console.WriteLine();
                        ttl++;
                    }
                }
            }
        }
    }
}
