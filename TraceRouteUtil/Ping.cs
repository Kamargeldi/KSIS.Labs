using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading;

namespace TraceRouteUtil
{
    public class Ping
    {
        private static readonly Dictionary<byte, string> messageTypes;
        private Socket socket;
        private IPEndPoint local;
        static Ping()
        {
            messageTypes = new Dictionary<byte, string>();
            messageTypes.Add(0, "Echo reply");
            messageTypes.Add(3, "Cannot reach endpoint");
            messageTypes.Add(5, "Readdress route");
            messageTypes.Add(8, "Echo request");
            messageTypes.Add(9, "Router message");
            messageTypes.Add(10, "Request message router");
            messageTypes.Add(11, "TTL exceeded");
            messageTypes.Add(12, "Parameters issues");
            messageTypes.Add(13, "Time mark request");
            messageTypes.Add(14, "Time mark reply");
        }
        public Ping()
        {
            #region GET LOCAL IP
            var ips = Dns.GetHostAddresses(Dns.GetHostName());
            var ip = ips[0];
            foreach (IPAddress address in ips)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                    ip = address;
            }
            #endregion
            local = new IPEndPoint(ip, 0);
        }

        private bool Send(IPAddress address, short ttl)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            socket.Bind(local);
            socket.Ttl = ttl;
            socket.ReceiveTimeout = 5000;
            #region GENERATE ICMP REQUEST PACKET
            byte[] sendData = new byte[12];
            sendData[0] = 8;    // type
            sendData[1] = 0;    // code
            sendData[2] = 0;    // checksum
            sendData[3] = 0;
            sendData[4] = 0;    // identifier
            sendData[5] = 0;
            sendData[6] = 0;    // sequence
            sendData[7] = 0;
            sendData[8] = 0;    // time
            sendData[9] = 0;
            sendData[10] = 0;
            sendData[11] = 0;
            int checksum = 0;
            for (int i = 0; i < 11; i++)
            {
                checksum += BitConverter.ToInt16(new byte[] { sendData[i], sendData[i + 1] });
            }
            sendData[2] = BitConverter.GetBytes(~checksum)[0];
            sendData[3] = BitConverter.GetBytes(~checksum)[1];

            #endregion

            byte[] buffer = new byte[128];
            EndPoint remote = new IPEndPoint(IPAddress.Any, 0);

            socket.SendTo(sendData.ToArray(), new IPEndPoint(address, 0));
            Console.WriteLine($"From:      {local.Address.ToString()}({Dns.GetHostName()})        To: {address}        TTL: {ttl}     MessageType: Echo request");

            try
            {
                socket.ReceiveFrom(buffer, ref remote);
            }
            catch { return false; }
            socket.Close();
            string remoteIp = $"{buffer[12]}.{buffer[13]}.{buffer[14]}.{buffer[15]}";
            string localIp = $"{buffer[16]}.{buffer[17]}.{buffer[18]}.{buffer[19]}";
            var remhost = "";
            try
            {
                remhost = Dns.GetHostEntry(IPAddress.Parse(remoteIp)).HostName;
            }
            catch { }
            Console.WriteLine($"Router:    {remoteIp}({remhost})  LocalComputer:   {localIp}({Dns.GetHostName()})    MessageType: {messageTypes[buffer[20]]}");
            if (buffer[20] == 0)
            {
                return true;
            }

            return false;
        }

        public bool PingRequest(IPAddress ip, short ttl, int times)
        {
            if (ip is null)
            {
                throw new ArgumentNullException(nameof(ip));
            }

            if (ttl < 1)
            {
                throw new ArgumentException($"Parameter {nameof(ttl)} cannot be less than 1");
            }

            if (times < 1)
            {
                throw new ArgumentException($"Parameter {nameof(times)} cannot be less than 1");
            }
            bool result = false;
            for (int i = 0; i < times; i++)
            {
                result = result || this.Send(ip, ttl);
            }

            return result;
        }

        public bool PingRequest(string hostName, short ttl, int times)
        {
            IPAddress ip = IPAddress.Any;
            var addressList = Dns.GetHostEntry(hostName).AddressList;
            foreach (IPAddress item in addressList)
            {
                if (item.AddressFamily == AddressFamily.InterNetwork)
                    ip = item;
            }

            return PingRequest(ip, ttl, times);
        }
    }
}
