using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SocketCore {
    public class UdpServer:IDisposable {
        /// <summary>是否可用</summary>
        public Boolean Useable{get;private set;}=false;
        /// <summary>客户端列表</summary>
        public Dictionary<EndPoint,UdpClientWrap> ClientSocketDictionary{get;private set;}=new Dictionary<EndPoint,UdpClientWrap>();

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
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。
                this.ClientSocketDictionary=null;
                
                disposedValue=true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        ~UdpServer(){
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
                this.ServerSocket=new Socket(AddressFamily.InterNetworkV6,SocketType.Dgram,ProtocolType.Udp){DualMode=true,Blocking=false};
                //this.ServerSocket.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.ReuseAddress,true);
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
            //完成
            Console.WriteLine(String.Concat("UdpServer Listen On [",address,"]:",port));
            this.Useable=true;
            this.ReceiveFromAsync();
            return true;
        }

        private async void ReceiveFromAsync() {
            if(!this.Useable){return;}
            //接受端点 [::]:0
            EndPoint anyEndPoint;
            try {
                anyEndPoint=new IPEndPoint(IPAddress.Any,0) as EndPoint;
            } catch {
                throw;
            }
            if(anyEndPoint==null){return;}
            //接受数据
            ArraySegment<Byte> buffer=new ArraySegment<Byte>(new Byte[4096]);
            SocketReceiveFromResult result=await this.ServerSocket.ReceiveFromAsync(buffer,SocketFlags.None,anyEndPoint).ConfigureAwait(false);
            EndPoint remoteEndPoint=result.RemoteEndPoint;
            if(!this.ClientSocketDictionary.ContainsKey(remoteEndPoint)){ this.ClientSocketDictionary[remoteEndPoint]=new UdpClientWrap{EndPoint=remoteEndPoint}; }
            this.OnReceiveFrom(this.ClientSocketDictionary[remoteEndPoint],result.ReceivedBytes,buffer.ToArray());
            buffer=null;
            //递归调用
            _=Task.Run(()=>{ this.ReceiveFromAsync(); });
        }

        private async void SendToAsync(UdpClientWrap udpClientWrap,Byte[] bytes){
            ArraySegment<Byte> buffer=new ArraySegment<Byte>(bytes);
            await this.ServerSocket.SendToAsync(buffer,SocketFlags.None,udpClientWrap.EndPoint).ConfigureAwait(false);
        }

        private void OnReceiveFrom(UdpClientWrap udpClientWrap,Int32 dataSize,Byte[] buffer){
            if(dataSize<1){return;}
            Byte[] bytes=new Byte[dataSize];
            Array.Copy(buffer,0,bytes,0,dataSize);
            udpClientWrap.ReceivedBytesList.Add(bytes);
            Console.WriteLine($"UdpServer => 第{udpClientWrap.ReceivedBytesList.Count}次收到来自{udpClientWrap.EndPoint.ToString()}的{dataSize}字节数组");
            this.SendToAsync(udpClientWrap,bytes);
        }
    }
}
