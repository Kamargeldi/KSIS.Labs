using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace P2P
{
    public class Node
    {
        
        public Node(string nick, IPAddress address, TcpClient connection)
        {
            Address = address;
            Nick = nick;
            Connection = connection;
        }

        public void TcpReceive(List<Node> clients, List<string> history)
        {
            NetworkStream nStream = Connection.GetStream(); // метод получения сообщения из потока tcp
            try
            {
                while (true)
                {
                    byte[] data = new byte[64];
                    StringBuilder sBuilder = new StringBuilder();
                    string message;
                    int readBytes = 0;
                    do
                    {
                        readBytes = nStream.Read(data, 0, data.Length);
                        sBuilder.Append(Encoding.UTF8.GetString(data, 0, readBytes));
                    }
                    while (nStream.DataAvailable);

                    message = sBuilder.ToString();
                    if (message != "/history")
                    {
                        if (Nick == null)
                        {
                            Nick = message;
                        }
                        else
                        {
                            Console.WriteLine($"{Nick} [ip: {Address.ToString()}] >> {message}");
                            history.Add($"{Nick} [ip: {Address.ToString()}] >> {message}\n");
                        }
                    }
                    else
                    {
                        byte[] historyData;
                        foreach (string item in history)
                        {
                            historyData = Encoding.ASCII.GetBytes(item);
                            this.Connection.GetStream().Write(historyData, 0, historyData.Length);
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine($"{Nick} [ip: {Address.ToString()}] left chat.");
                history.Add($"{Nick} [ip: {Address.ToString()}] left chat.\n");
                var address = ((IPEndPoint)Connection.Client.RemoteEndPoint).Address;
                lock (Program.mutex)
                {
                    clients.RemoveAll(X => X.Address.ToString() == address.ToString());
                }
            }
            finally
            {
                if (nStream != null)
                    nStream.Close();
                if (Connection != null)
                    Connection.Close();

            }
        }

        public IPAddress Address;
        public string Nick;
        public TcpClient Connection;
    }


    public class Utils
    {
        
        public static IPAddress GetLocalIpAddress()
        {
            foreach (var netI in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (netI.NetworkInterfaceType != NetworkInterfaceType.Wireless80211 &&
                    (netI.NetworkInterfaceType != NetworkInterfaceType.Ethernet ||
                     netI.OperationalStatus != OperationalStatus.Up)) continue;
                foreach (var uniIpAddrInfo in netI.GetIPProperties().UnicastAddresses.Where(x => netI.GetIPProperties().GatewayAddresses.Count > 0))
                {

                    if (uniIpAddrInfo.Address.AddressFamily == AddressFamily.InterNetwork &&
                        uniIpAddrInfo.AddressPreferredLifetime != uint.MaxValue)
                        return uniIpAddrInfo.Address;
                }
            }

            return null;
        }

        public static IPAddress GetSubnetMask(IPAddress address)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (address.Equals(unicastIPAddressInformation.Address))
                        {
                            return unicastIPAddressInformation.IPv4Mask;
                        }
                    }
                }
            }
            throw new ArgumentException(string.Format("Can't find subnetmask for IP address '{0}'", address));
        }



        public static IPAddress GetBroadcast(IPAddress address, IPAddress subnet)
        {
            string[] result;
            string[] addr;
            string[] sub;
            addr = address.ToString().Split('.');
            sub = subnet.ToString().Split('.');
            result = new string[4];
            for (int i = 0; i < 4; i++)
            {
                result[i] = (int.Parse(sub[i]) & int.Parse(addr[i])).ToString();
                if (int.Parse(sub[i]) == 0)
                {
                    result[i] = "255";
                }
            }

            return IPAddress.Parse(string.Join(".", result));
        }

    }

}
