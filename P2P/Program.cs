using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace P2P
{
    class Program
    {
        public static object mutex = new object();
        const int TcpPort = 1267;
        const int UdpPort = 1268;
        static bool exit = false;


        static void Main(string[] args)
        {
            IPAddress broadcastAddr = IPAddress.Parse("192.168.43.255");
            IPEndPoint broadcastEP = new IPEndPoint(broadcastAddr, UdpPort);
            Thread UdpListenThread;
            UdpClient UdpListener;
            UdpClient UdpSender;
            TcpListener tcpListener;
            Thread TcpListenThread;
            List<string> History = new List<string>();
            List<Node> Contacts = new List<Node>();
            string myNick;

            Console.Write("Enter your Nick:  ");
            myNick = Console.ReadLine();

            try
            {
                UdpSender = new UdpClient(UdpPort, AddressFamily.InterNetwork);
                IPAddress myIp = Utils.GetLocalIpAddress();
                byte[] myNickBytes = Encoding.UTF8.GetBytes(myNick);
                int sentBytes = UdpSender.Send(myNickBytes, myNickBytes.Length, broadcastEP);
                if (sentBytes == myNickBytes.Length)
                {
                    Console.WriteLine($"{myNick} [ip: {myIp}] joined to chat.");
                    History.Add($"{myNick} [ip: {myIp}] joined to chat.");
                }

                UdpSender.Close();
            }
            catch
            {
                Console.WriteLine("Network error [udp sender].");
            }
            
            UdpListenThread = new Thread(() =>
            {
                UdpListener = new UdpClient();
                try
                {
                    IPEndPoint clientEP = new IPEndPoint(IPAddress.Any, UdpPort);
                    UdpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    UdpListener.ExclusiveAddressUse = false;
                    UdpListener.Client.Bind(clientEP);
                    
                    while (true)
                    {
                        byte[] data = UdpListener.Receive(ref clientEP);
                        string clientNick = Encoding.ASCII.GetString(data);
                        Contacts.Add(new Node(clientNick, clientEP.Address, null));
                        TcpClient tcpConnection = new TcpClient();
                        tcpConnection.Connect(new IPEndPoint(clientEP.Address, TcpPort));

                        Contacts[Contacts.Count - 1].Connection = tcpConnection;

                        Console.WriteLine($"{clientNick} [ip: {clientEP.Address.ToString()}] joined to chat.");
                        History.Add($"{clientNick} [ip: {clientEP.Address.ToString()}] joined to chat.\n");

                        Thread thread = new Thread(() =>
                        {
                            Contacts[Contacts.Count - 1].TcpReceive(Contacts, History);
                        });
                        thread.Start();

                        byte[] nickBytes = Encoding.ASCII.GetBytes(myNick);
                        tcpConnection.GetStream().Write(nickBytes, 0, nickBytes.Length);
                    }
                }
                catch
                {
                    Console.WriteLine("Network error [udp thread].");
                }
            });
            UdpListenThread.Start();

            TcpListenThread = new Thread(() =>
            {
                
                tcpListener = new TcpListener(IPAddress.Any, TcpPort);
                tcpListener.Start();
                try
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    var address = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                    Node searched = Contacts.Find(x => x.Address == address);
                    if (searched == null)                     
                    {
                        lock (Program.mutex)
                        {       
                            Node item = new Node(null, address, client);
                            Contacts.Add(item);
                            searched = item;
                        }
                    }

                    Thread thread = new Thread(()=>
                    {
                        searched.TcpReceive(Contacts, History);
                    });
                    thread.IsBackground = true;
                    thread.Start();
                }
                catch
                {
                    Console.WriteLine("Network error [tcp thread].");
                }
                finally
                {
                    tcpListener.Stop();
                }
            });
            TcpListenThread.Start();

            while (!exit)
            {
                string msg = Console.ReadLine();
                if (msg == "/exit")
                {
                    //TcpListenThread.IsBackground = true;
                    //UdpListenThread.IsBackground = true;
                    Console.WriteLine($"{myNick} [ip: {Utils.GetLocalIpAddress()}] left chat");
                    exit = true;
                }
                else
                {
                    byte[] data = Encoding.UTF8.GetBytes(msg);
                    foreach (Node item in Contacts)
                    {
                        item.Connection.GetStream().Write(data, 0, data.Length);
                    }

                    Console.WriteLine($"{myNick} [ip: {Utils.GetLocalIpAddress().ToString()}] >> {msg}");
                    History.Add($"{myNick} [ip: {Utils.GetLocalIpAddress().ToString()}] >> {msg}\n");
                }
            }

            Console.ReadKey();
        }
    }
}
