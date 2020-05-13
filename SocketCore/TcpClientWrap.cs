using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketCore {
    public class TcpClientWrap {
        /// <summary>客户端端点</summary>
        public EndPoint EndPoint{get;set;}=null;
        /// <summary>客户端套接字</summary>
        public Socket ClientSocket{get;set;}=null;
        /// <summary>来自客户端的数据列表</summary>
        public List<Byte[]> ReceivedBytesList{get;}=new List<Byte[]>();
    }
}
