using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net;

namespace TracerouteUtil
{
    /// <summary>
    /// Custom ICMPRequest class.
    /// </summary>
    public static class ICMPRequest
    {   
        /// <summary>
        /// Sends ping request until reach the destination
        /// and prints ip of each router.
        /// </summary>
        /// <param name="hostName">Target hostname.</param>
        public static void Send(string hostName)
        {
            PingOptions pingOptions = new PingOptions();
            Ping ping = new Ping();
            PingReply reply;
            int ttl = 1;
            string result = "";
            IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
            IPAddress localIp = IPAddress.Any;
            foreach (IPAddress item in ips)
            {
                if (item.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIp = item;
                }
            }

            do
            {
                pingOptions.Ttl = ttl;
                reply = ping.Send(hostName, 5000, new byte[] { 1 }, pingOptions);
                result = " ping request ttl: " + ttl.ToString()
                     + " status: " + reply.Status.ToString() + " time: "
                    + reply.RoundtripTime + "ms from "
                    + localIp.ToString() + " to ";
                if (reply.Address != null)
                {
                    result += reply.Address.ToString();
                }
                else
                {
                    result += "**********";
                }
                Console.WriteLine(result);
                ttl++;
            }
            while (reply.Status != IPStatus.Success);
        }

        /// <summary>
        /// Sends ping request until reach the destination
        /// and prints ip of each router.
        /// </summary>
        /// <param name="ip">Target IP Address.</param>
        public static void Send(IPAddress ip)
        {
            PingOptions pingOptions = new PingOptions();
            Ping ping = new Ping();
            PingReply reply;
            int ttl = 1;
            string result = "";
            IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
            IPAddress localIp = IPAddress.Any;
            foreach (IPAddress item in ips)
            {
                if (item.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIp = item;
                }
            }

            do
            {
                pingOptions.Ttl = ttl;
                reply = ping.Send(ip, 5000, new byte[] { 1 }, pingOptions);
                result = " ping request ttl: " + ttl.ToString()
                     + " status: " + reply.Status.ToString() + " time: "
                    + reply.RoundtripTime + "ms from "
                    + localIp.ToString() + " to ";
                if (reply.Address != null)
                {
                    result += reply.Address.ToString();
                }
                else
                {
                    result += "**********";
                }
                Console.WriteLine(result);
                ttl++;
            }
            while (reply.Status != IPStatus.Success);
        }

        /// <summary>
        /// Sends ping request until reach the destination
        /// and prints ip address with hostname of each router.
        /// (hostname also cannot be found).
        /// </summary>
        /// <param name="hostName">Target hostname.</param>
        public static void SendWithDNS(string hostName)
        {
            PingOptions pingOptions = new PingOptions();
            Ping ping = new Ping();
            PingReply reply;
            int ttl = 1;
            string result = "";
            IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
            IPAddress localIp = IPAddress.Any;
            foreach (IPAddress item in ips)
            {
                if (item.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIp = item;
                }
            }

            do
            {
                pingOptions.Ttl = ttl;
                reply = ping.Send(hostName, 5000, new byte[] { 1 }, pingOptions);
                result = " ping request ttl: " + ttl.ToString()
                     + " status: " + reply.Status.ToString() + " time: "
                    + reply.RoundtripTime + "ms from "
                    + $"({Dns.GetHostName()})" + localIp.ToString() + " to ";
                if (reply.Address != null)
                {
                    string hName;
                    try
                    {
                        hName = $"({Dns.GetHostEntry(reply.Address).HostName})";
                    }
                    catch
                    {
                        hName = string.Empty;
                    }

                    result += hName;
                    result += reply.Address.ToString();
                }
                else
                {
                    result += "**********";
                }
                Console.WriteLine(result);
                ttl++;
            }
            while (reply.Status != IPStatus.Success);
        }

        /// <summary>
        /// Sends ping request until reach the destination
        /// and prints ip address with hostname of each router.
        /// (hostname also cannot be found).
        /// </summary>
        /// <param name="ip">Target IP Address.</param>
        public static void SendWithDNS(IPAddress ip)
        {
            PingOptions pingOptions = new PingOptions();
            Ping ping = new Ping();
            PingReply reply;
            int ttl = 1;
            string result = "";
            IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
            IPAddress localIp = IPAddress.Any;
            foreach (IPAddress item in ips)
            {
                if (item.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIp = item;
                }
            }

            do
            {
                pingOptions.Ttl = ttl;
                reply = ping.Send(ip, 5000, new byte[] { 1 }, pingOptions);
                result = " ping request ttl: " + ttl.ToString()
                     + " status: " + reply.Status.ToString() + " time: "
                    + reply.RoundtripTime + "ms from "
                    + $"({Dns.GetHostName()})" + localIp.ToString() + " to ";
                if (reply.Address != null)
                {
                    string hName;
                    try
                    {
                        hName = $"({Dns.GetHostEntry(reply.Address).HostName})";
                    }
                    catch
                    {
                        hName = string.Empty;
                    }

                    result += hName;
                    result += reply.Address.ToString();
                }
                else
                {
                    result += "**********";
                }
                Console.WriteLine(result);
                ttl++;
            }
            while (reply.Status != IPStatus.Success);
        }
    }
}
