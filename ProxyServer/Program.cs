
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;
using System.Threading;

namespace ProxyServer
{
    class Program
    {

        private static string proxyAddress = "127.0.0.1";
        private static int proxyPort = 8888; 

        private static bool needAuth = false; 
        private static string login = "test"; 
        private static string password = "123123"; 

        private static bool appendHtml = true; 

        private static bool allowBlackList = true;
        private static string[] blackList = null; 

        static void Main(string[] args)
        {
            if (allowBlackList && File.Exists("BlackList.txt"))
            {
                blackList = File.ReadAllLines("BlackList.txt");
            }

            TcpListener myTCP = new TcpListener(IPAddress.Parse(proxyAddress), proxyPort);

            myTCP.Start();

            WriteLog("Прокси-сервер запущен, слушаем {0} порт {1}.", proxyAddress, proxyPort);
            while (true)
            {
                if (myTCP.Pending())
                {
                    Thread t = new Thread(ExecuteRequest);
                    t.IsBackground = true;
                    t.Start(myTCP.AcceptSocket());
                }
            }
        }

        private static void ExecuteRequest(object arg)
        {
            try
            {
                using (Socket myClient = (Socket)arg)
                {
                    if (myClient.Connected)
                    {
                        byte[] httpRequest = ReadToEnd(myClient);
                        HTTP.Parser http = new HTTP.Parser(httpRequest);
                        if (http.Items == null || http.Items.Count <= 0 || !http.Items.ContainsKey("Host"))
                        {
                            WriteLog("Получен запрос {0} байт, заголовки не найдены.", httpRequest.Length);
                        }
                        else
                        {
                            if (http.Method != HTTP.Parser.MethodsList.CONNECT)
                            {
                                WriteLog("Получен запрос {0} байт, метод {1}, хост {2}:{3}", httpRequest.Length, http.Method, http.Host, http.Port);
                            }

                            byte[] response = null;

                            if (needAuth)
                            {
                                if (!http.Items.ContainsKey("Authorization"))
                                {
                                    response = GetHTTPError(401, "Unauthorized");
                                    myClient.Send(response, response.Length, SocketFlags.None);
                                    return;
                                }
                                else
                                {
                                    string auth = Encoding.UTF8.GetString(Convert.FromBase64String(http.Items["Authorization"].Source.Replace("Basic ", "")));
                                    string _login = auth.Split(":".ToCharArray())[0];
                                    string pwd = auth.Split(":".ToCharArray())[1];
                                    if (login != _login || password != pwd)
                                    {
                                        response = GetHTTPError(401, "Unauthorized");
                                        myClient.Send(response, response.Length, SocketFlags.None);
                                        return;
                                    }
                                }
                            }

                            if (allowBlackList && blackList != null && Array.IndexOf(blackList, http.Host.ToLower()) != -1)
                            {
                                response = GetHTTPError(403, "Forbidden");
                                myClient.Send(response, response.Length, SocketFlags.None);
                                return;
                            }
                            
                            IPHostEntry myIPHostEntry = Dns.GetHostEntry(http.Host);

                            if (myIPHostEntry == null || myIPHostEntry.AddressList == null || myIPHostEntry.AddressList.Length <= 0)
                            {
                                WriteLog("Не удалось определить IP-адрес по хосту {0}.", http.Host);
                            }
                            else
                            {
                                IPEndPoint myIPEndPoint = new IPEndPoint(myIPHostEntry.AddressList[0], http.Port);
 
                                if (http.Method == HTTP.Parser.MethodsList.CONNECT)
                                {
                                    //WriteLog("Протокол HTTPS не реализован.");
                                    response = GetHTTPError(501, "Not Implemented");
                                }
                                else
                                {
                                    using (Socket myRerouting = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                                    {
                                        myRerouting.Connect(myIPEndPoint);
                                        if (myRerouting.Send(httpRequest, httpRequest.Length, SocketFlags.None) != httpRequest.Length)
                                        {
                                            WriteLog("Данные хосту {0} не были отправлены...", http.Host);
                                        }
                                        else
                                        {
                                            HTTP.Parser httpResponse = new HTTP.Parser(ReadToEnd(myRerouting));
                                            if (httpResponse.Source != null && httpResponse.Source.Length > 0)
                                            {
                                                WriteLog("Получен ответ {0} байт, код состояния {1}", httpResponse.Source.Length, httpResponse.StatusCode);
                                                response = httpResponse.Source;

                                                switch (httpResponse.StatusCode)
                                                {
                                                    case 400:
                                                    case 403:
                                                    case 404:
                                                    case 407:
                                                    case 500:
                                                    case 501:
                                                    case 502:
                                                    case 503:
                                                        response = GetHTTPError(httpResponse.StatusCode, httpResponse.StatusMessage);
                                                        break;

                                                    default:
                                                        if (appendHtml)
                                                        {
                                                            if (httpResponse.Items.ContainsKey("Content-Type") && ((HTTP.ItemContentType)httpResponse.Items["Content-Type"]).Value == "text/html")
                                                            {
                                                                string body = httpResponse.GetBodyAsString();

                                                                body = Regex.Replace(body, "<title>(?<title>.*?)</title>", "<title>ProxyServer - $1</title>");

                                                                body = Regex.Replace(body, "(<body.*?>)", "$1<div style='height:20px;width:100%;background-color:black;color:white;font-weight:bold;text-align:center;'>Example of Proxy Server by Aleksey Nemiro</div>");

                                                                httpResponse.SetStringBody(body);

                                                                response = httpResponse.Source;
                                                            }
                                                        }
                                                        
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                WriteLog("Получен ответ 0 байт");
                                            }
                                        }

                                        myRerouting.Close();
                                    }
                                } 
                                
                                if (response != null) myClient.Send(response, response.Length, SocketFlags.None);
                            }
                        }

                        myClient.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog("Ошибка: ", ex.Message);
            }
        }

        private static byte[] ReadToEnd(Socket mySocket)
        {
            byte[] b = new byte[mySocket.ReceiveBufferSize];
            int len = 0;
            using (MemoryStream m = new MemoryStream())
            {
                while (mySocket.Poll(1000000, SelectMode.SelectRead) && (len = mySocket.Receive(b, mySocket.ReceiveBufferSize, SocketFlags.None)) > 0)
                {
                    m.Write(b, 0, len);
                }
                return m.ToArray();
            }
        }

        private static byte[] GetHTTPError(int statusCode, string statusMessage)
        {
            FileInfo FI = new FileInfo(String.Format("HTTP{0}.htm", statusCode));
            byte[] headers = Encoding.ASCII.GetBytes(String.Format("HTTP/1.1 {0} {1}\r\n{3}Content-Type: text/html\r\nContent-Length: {2}\r\n\r\n", statusCode, statusMessage, FI.Length, (statusCode == 401 ? "WWW-Authenticate: Basic realm=\"ProxyServer Example\"\r\n" : "")));
            byte[] result = null;

            using (FileStream fs = new FileStream(FI.FullName, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs, Encoding.UTF8))
                {
                    result = new byte[headers.Length + fs.Length];
                    Buffer.BlockCopy(headers, 0, result, 0, headers.Length);
                    Buffer.BlockCopy(br.ReadBytes(Convert.ToInt32(fs.Length)), 0, result, headers.Length, Convert.ToInt32(fs.Length));
                }
            }

            return result;
        }

        private static void WriteLog(string msg, params object[] args)
        {
            Console.WriteLine(DateTime.Now.ToString() + " : " + msg, args);
        }
    }
}
