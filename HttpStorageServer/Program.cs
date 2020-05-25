using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.IO;

namespace HttpStorageServer
{
    class Program
    {
        public static object mutex = new object();
        public static int listenPort = 1267;
        public static string rootDir = @"E:\files";

        static void Main(string[] args)
        {
            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://{Utils.GetLocalIpAddress()}:{listenPort}/");
            httpListener.Start();
            Console.WriteLine($">listening   http://{Utils.GetLocalIpAddress()}:{listenPort}/");
            while (true)
            {
                HttpListenerContext requestContext = httpListener.GetContext();
                Thread thread = new Thread(() => { ExecuteRequest(requestContext); });
                thread.Start();
            }
            
        }


        private static void ExecuteRequest(HttpListenerContext context)
        {
            string httpMethod = context.Request.HttpMethod;
            switch(httpMethod.ToUpper())
            {
                case "GET":
                    GetFile(context);
                    break;
                case "PUT":
                    PostFile(context);
                    break;
                case "DELETE":
                    DeleteFile(context);
                    break;
                case "HEAD":
                    GetMetaData(context);
                    break;
            }
        }
        private static void PostFile(HttpListenerContext context)
        {
            var filename = context.Request.Url.AbsolutePath;
            filename = filename.Replace(@"/", @"\");
            filename = rootDir + filename;
            string responseMessage = "";
            int statusCode = 0;

            if (context.Request.Headers.AllKeys.Contains("X-Copy-From"))
            {
                int result = CopyFile(filename, rootDir + context.Request.Headers.Get("X-Copy-From").Replace(@"/", @"\"));
                switch (result)
                {
                    case 1:
                        responseMessage = Utils.GetHtmlText($"File not found {filename}.");
                        statusCode = (int)(HttpStatusCode.NotFound);
                        break;
                    case 2:
                        responseMessage = Utils.GetHtmlText($"File already exists {context.Request.Headers.Get("X-Copy-From")}");
                        statusCode = (int)HttpStatusCode.BadRequest;
                        break;
                    case 3:
                        responseMessage = Utils.GetHtmlText($"File wrong path or name {context.Request.Headers.Get("X-Copy-From")}");
                        statusCode = (int)HttpStatusCode.BadRequest;
                        break;
                    case 0:
                        responseMessage = Utils.GetHtmlText($"File copied from {filename} to {context.Request.Headers.Get("X-Copy-From")}");
                        statusCode = (int)HttpStatusCode.OK;
                        break;
                }

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = Utils._mimeTypeMappings[".html"];
                context.Response.ContentLength64 = Encoding.UTF8.GetBytes(responseMessage).Length;
                context.Response.OutputStream.Write(Encoding.UTF8.GetBytes(responseMessage), 0, Encoding.UTF8.GetBytes(responseMessage).Length);
                context.Response.Close();
                return;
            }

            if (File.Exists(filename))
            {
                responseMessage = Utils.GetHtmlText($"File rewritten {context.Request.Url.AbsolutePath}.");
                statusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                responseMessage = Utils.GetHtmlText($"File created {context.Request.Url.AbsolutePath}");
                statusCode = (int)HttpStatusCode.Created;
            }

            FileInfo fInfo = new FileInfo(filename);
            if (!fInfo.Directory.Exists)
                Directory.CreateDirectory(fInfo.DirectoryName);
            FileStream fs = new FileStream(filename, FileMode.Create);
            byte[] buffer = new byte[1024 * 16];
            var postDataStream = context.Request.InputStream;
            int nbytes;
            while ((nbytes = postDataStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                fs.Write(buffer, 0, nbytes);
                fs.Seek(nbytes, SeekOrigin.Begin);
            }

            fs.Close();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = Utils._mimeTypeMappings[".html"];
            context.Response.ContentLength64 = Encoding.UTF8.GetBytes(responseMessage).Length;
            context.Response.OutputStream.Write(Encoding.UTF8.GetBytes(responseMessage), 0, Encoding.UTF8.GetBytes(responseMessage).Length);
            context.Response.Close();

        }
        private static void DeleteFile(HttpListenerContext context)
        {
            string filename = context.Request.Url.AbsolutePath;
            filename = filename.Replace(@"/", @"\");
            filename = rootDir + filename;

            if (File.Exists(filename))
            {
                File.Delete(filename);
                byte[] buffer = Encoding.UTF8.GetBytes(Utils.GetHtmlText($"File deleted {context.Request.Url.AbsolutePath}."));
                context.Response.ContentLength64 = buffer.Length;
                context.Response.ContentType = Utils._mimeTypeMappings[".html"];
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            else
            {
                byte[] buffer = Encoding.UTF8.GetBytes(Utils.GetHtmlText($"File not found {context.Request.Url.AbsolutePath}."));
                context.Response.ContentLength64 = buffer.Length;
                context.Response.ContentType = Utils._mimeTypeMappings[".html"];
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            }

            context.Response.Close();
        }
        private static void GetFile(HttpListenerContext context)
        {
            string filename = context.Request.Url.AbsolutePath;
            
            if (filename == "/extra/dirinfo.json")
            {
                var buffer = Encoding.UTF8.GetBytes(Utils.GetDirInfoJSON(rootDir));
                context.Response.ContentLength64 = buffer.Length;
                context.Response.ContentType = Utils._mimeTypeMappings[".json"];
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.Close();
                return;
            }

            filename = filename.Replace(@"/", @"\");

            filename = rootDir + filename;

            if (File.Exists(filename))
            {
                try
                {
                    Stream input = new FileStream(filename, FileMode.Open);

                    string mime;
                    context.Response.ContentType = Utils._mimeTypeMappings.TryGetValue(Path.GetExtension(filename), out mime) ? mime : "application/octet-stream";
                    context.Response.ContentLength64 = input.Length;
                    context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                    context.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(filename).ToString("r"));
                    context.Response.StatusCode = (int)HttpStatusCode.OK;

                    byte[] buffer = new byte[1024 * 16];
                    int nbytes;
                    while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                        context.Response.OutputStream.Write(buffer, 0, nbytes);
                    input.Close();

                    context.Response.OutputStream.Flush();
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }

            }
            else
            {
                byte[] buffer = Encoding.UTF8.GetBytes(Utils.GetHtmlText($"File not found {context.Request.Url.AbsolutePath}."));
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.ContentType = Utils._mimeTypeMappings[".html"];
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            }

            context.Response.OutputStream.Close();
        }
        private static void GetMetaData(HttpListenerContext context)
        {
            string filename = context.Request.Url.AbsolutePath;
            filename = filename.Replace(@"/", @"\");


            filename = rootDir + filename;

            if (File.Exists(filename))
            {
                context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                context.Response.AddHeader("Last-Modified", File.GetLastWriteTime(filename).ToString());
                context.Response.AddHeader("Allow", "GET, HEAD, PUT, DELETE");
                context.Response.AddHeader("Age", $"{(DateTime.Now - File.GetLastWriteTime(filename)).TotalSeconds}");
                context.Response.AddHeader("Content-Location", context.Request.Url.AbsolutePath);
                string mime;
                context.Response.ContentType = Utils._mimeTypeMappings.TryGetValue(Path.GetExtension(filename), out mime) ? mime : "application/octet-stream";
                context.Response.ContentLength64 = new FileInfo(filename).Length;
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.Close();
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.Close();
            }
        }
        private static int CopyFile(string pathFrom, string pathTo)
        {
            if (!File.Exists(pathFrom))
                return 1;                      //file1 not found
            if (File.Exists(pathTo))           
                return 2;                      //file2 exists (conflict)
            try
            {
                var dirTo = new FileInfo(pathTo);
                if (!dirTo.Directory.Exists)
                    Directory.CreateDirectory(dirTo.DirectoryName);

                File.Copy(pathFrom, pathTo);
            }
            catch (Exception ex)
            {
                return 3;                      //wrong file2 path
            }
            return 0;                          //success
        }
        
    }
}
