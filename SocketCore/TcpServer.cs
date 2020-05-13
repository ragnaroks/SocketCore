using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SocketCore {
    public class TcpServer:IDisposable {
        /// <summary>是否可用</summary>
        public Boolean Useable{get;private set;}=false;
        /// <summary>客户端列表</summary>
        public Dictionary<EndPoint,TcpClientWrap> ClientSocketDictionary{get;private set;}=new Dictionary<EndPoint,TcpClientWrap>();

        /// <summary>编码</summary>
        private Encoding Encoding{get;}=Encoding.UTF8;
        /// <summary>套接字</summary>
        private Socket ServerSocket{get;set;}=null;

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing) {
            if(!disposedValue) {
                if(disposing) {
                    // TODO: 释放托管状态(托管对象)。
                    if(this.ServerSocket!=null){ this.ServerSocket.Dispose(); }
                    if(this.ClientSocketDictionary.Count>0) {
                        foreach(KeyValuePair<EndPoint,TcpClientWrap> item in this.ClientSocketDictionary){
                            item.Value.ClientSocket.Dispose();
                        }
                    }
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。
                this.ClientSocketDictionary=null;

                disposedValue=true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        ~TcpServer(){
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(false);
        }

        // 添加此代码以正确实现可处置模式。
        public void Dispose() {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            GC.SuppressFinalize(this);
        }
        #endregion

        /// <summary>
        /// 初始化套接字
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="port">端口</param>
        /// <exception cref="Exception"></exception>
        public Boolean Setup(String address,Int16 port){
            //防止重复调用
            if(this.Useable){return true;}
            //地址对象
            Boolean b1;
            IPAddress ipAddress;
            try{
                b1=IPAddress.TryParse(address,out ipAddress);
            }catch{
                throw;
            }
            if(!b1 || ipAddress==null){return false;}
            //监听端点
            IPEndPoint ipEndPoint;
            try{
                ipEndPoint=new IPEndPoint(ipAddress,port);
            }catch{
                throw;
            }
            if(ipEndPoint==null){return false;}
            //初始化套接字
            try{
                this.ServerSocket=new Socket(AddressFamily.InterNetworkV6,SocketType.Stream,ProtocolType.Tcp){DualMode=true,Blocking=false};
            }catch{
                throw;
            }
            if(this.ServerSocket==null){return false;}
            //绑定端点
            try {
                this.ServerSocket.Bind(ipEndPoint);
            }catch{
                throw;
            }
            //监听,队列128
            this.ServerSocket.Listen(128);
            //完成
            Console.WriteLine(String.Concat("TcpServer Listen On [",address,"]:",port));
            this.Useable=true;
            return true;
        }

        public async void AcceptAsync() {
            Socket clientSocket=await this.ServerSocket.AcceptAsync().ConfigureAwait(false);
            EndPoint remoteEndPoint=clientSocket.RemoteEndPoint;
            if(this.ClientSocketDictionary.ContainsKey(remoteEndPoint)) {
                this.ClientSocketDictionary[remoteEndPoint].ClientSocket.Dispose();
                this.ClientSocketDictionary.Remove(remoteEndPoint);
            }
            this.ClientSocketDictionary[remoteEndPoint]=new TcpClientWrap{ClientSocket=clientSocket,EndPoint=remoteEndPoint};
            this.ReceiveAsync(this.ClientSocketDictionary[remoteEndPoint]);
            //递归调用
            _=Task.Run(()=>{ this.AcceptAsync(); });
        }

        public async void ReceiveAsync(TcpClientWrap tcpClientWrap){
            if(!this.Useable){return;}
            if(tcpClientWrap==null){return;}
            ArraySegment<Byte> buffer=new ArraySegment<Byte>(new Byte[4096]);
            Int32 size=0;
            try {
                size=await tcpClientWrap.ClientSocket.ReceiveAsync(buffer,SocketFlags.None).ConfigureAwait(false);
            } catch(Exception exception){
                this.ClientSocketDictionary[tcpClientWrap.EndPoint].ClientSocket.Dispose();
                this.ClientSocketDictionary.Remove(tcpClientWrap.EndPoint);
                Console.WriteLine($"TcpServer => {tcpClientWrap.EndPoint.ToString()}因为\"{exception.Message}\"而断开链接");
            }
            if(size<1){return;}
            this.OnReceiveFrom(tcpClientWrap,size,buffer.ToArray());
            //递归调用
            _=Task.Run(()=>{ this.ReceiveAsync(tcpClientWrap); });
        }

        private async void SendAsync(TcpClientWrap tcpClientWrap,Byte[] bytes){
            if(tcpClientWrap==null){return;}
            ArraySegment<Byte> buffer=new ArraySegment<Byte>(bytes);
            await tcpClientWrap.ClientSocket.SendAsync(buffer,SocketFlags.None).ConfigureAwait(false);
        }

        private void OnReceiveFrom(TcpClientWrap tcpClientWrap,Int32 dataSize,Byte[] buffer){
            Byte[] bytes=new Byte[dataSize];
            Array.Copy(buffer,0,bytes,0,dataSize);
            tcpClientWrap.ReceivedBytesList.Add(bytes);
            Console.WriteLine($"TcpServer => 第{tcpClientWrap.ReceivedBytesList.Count}次收到来自{tcpClientWrap.EndPoint.ToString()}的{dataSize}字节数组");
            this.SendAsync(tcpClientWrap,bytes);
        }
    }
}

