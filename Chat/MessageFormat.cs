using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace Chat
{
    [Serializable]
    public class MessageFormat
    {
        public int Type;
        public string Message;
        public IPAddress Address;
    }
}
