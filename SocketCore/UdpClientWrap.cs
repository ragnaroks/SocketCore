using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketCore {
    public class UdpClientWrap {
        /// <summary>客户端端点</summary>
        public EndPoint EndPoint{get;set;}=null;
        /// <summary>来自客户端的数据列表</summary>
        public List<Byte[]> ReceivedBytesList{get;}=new List<Byte[]>();
    }
}
