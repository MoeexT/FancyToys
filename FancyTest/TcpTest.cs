
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace FancyTest {
    [TestClass]
    public class TcpTest {
        [TestMethod]
        public void SocketSynchronousSend() {
            Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(IPAddress.Parse("43.139.72.27"), 7878);
            Console.WriteLine(_socket.Connected);

            _socket.Send(Encoding.UTF8.GetBytes("Hello"));

            byte[] buf = new byte[1024];
            int len = _socket.Receive(buf);
            byte[] bytes = new byte[len];
            Array.Copy(buf, 0, bytes, 0, len);
            Console.WriteLine(Encoding.UTF8.GetString(bytes));
        }

        [TestMethod]
        public void TcpClientSend() {
            byte[] buf = new byte[64];
            
            var ms = new MemoryStream();
            ms.Write(Encoding.UTF8.GetBytes("Hello Rust"));
            
            var client = new TcpClient("43.139.72.27", 7878);
            var ns = client.GetStream();
            
            Console.WriteLine($"{ms.Length}, {ms.Position}");
            ms.Seek(0, SeekOrigin.Begin);
            Console.WriteLine($"{ms.Length}, {ms.Position}");
            ms.CopyTo(ns);

            // ns.Write(Encoding.UTF8.GetBytes("Hello Rust"));
            int len = ns.Read(buf);
            byte[] bytes = new byte[len];
            Array.Copy(buf, 0, bytes, 0, len);
            Console.WriteLine(Encoding.UTF8.GetString(bytes));
            // ns.Flush();
            
            client.Close();
        }
    }
}
