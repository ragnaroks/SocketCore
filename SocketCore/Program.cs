using System;

namespace SocketCore {
    public class Program {
        private static String Address{get;set;}="::";
        private static Int16 Port{get;set;}=9527;
        private static UdpServer UdpServer{get;set;}=new UdpServer();
        private static TcpServer TcpServer{get;set;}=new TcpServer();

        public static void Main(String[] args) {
            Console.WriteLine("Hello World!");

            try {
                if(!Program.UdpServer.Setup(Program.Address,Program.Port)){ Console.WriteLine("初始化 UdpServer 失败"); }
            }catch(Exception exception) {
                Console.Error.WriteLine(String.Concat("初始化 UdpServer 异常,",exception.Message,",",exception.StackTrace));
            }

            try {
                if(!Program.TcpServer.Setup(Program.Address,Program.Port)){
                    Console.WriteLine("初始化 TcpServer 失败");
                }else{
                    Program.TcpServer.AcceptAsync();
                }
            }catch(Exception exception) {
                Console.Error.WriteLine(String.Concat("初始化 TcpServer 异常,",exception.Message,",",exception.StackTrace));
            }
            
            Console.ReadLine();

            if(Program.UdpServer.Useable){ Program.UdpServer.Dispose(); }

            if(Program.TcpServer.Useable){ Program.TcpServer.Dispose(); }
        }
    }
}
