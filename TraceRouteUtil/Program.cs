using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;


namespace TracerouteUtil
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.Write(">  ");
                var command = Console.ReadLine().Split(' ');
                if (command.Length != 2)
                {
                    Console.WriteLine("Run each command with 1 parameter.");
                    continue;
                }

                if (command[0] != "tracert" && command[0] != "tracertdns")
                {
                    Console.WriteLine("command " + command[0] + " not found.");
                    continue;
                }
                if (command[0] == "tracert")
                {
                    try
                    {
                        IPAddress argIp;
                        if (IPAddress.TryParse(command[1], out argIp))
                        {
                            ICMPRequest.Send(argIp);
                        }
                        else
                        {
                            ICMPRequest.Send(command[1]);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                
                if (command[0] == "tracertdns")
                {
                    try
                    {
                        IPAddress argIp;
                        if (IPAddress.TryParse(command[1], out argIp))
                        {
                            ICMPRequest.SendWithDNS(argIp);
                        }
                        else
                        {
                            ICMPRequest.SendWithDNS(command[1]);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }
    }
}
