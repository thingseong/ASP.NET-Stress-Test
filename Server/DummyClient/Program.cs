// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Hello, World!");

try
{
    string host = Dns.GetHostName();
    Console.WriteLine($"Host: {host}");
    IPHostEntry ipHostEntry = Dns.GetHostEntry(host);
    IPAddress ipAddress = ipHostEntry.AddressList[0];
    IPEndPoint endPoint = new IPEndPoint(ipAddress, 7777);
    
    while (true)
    {
        Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(endPoint);
        Console.WriteLine($"Socket connected to {socket.RemoteEndPoint}");

        for (int i = 0; i < 5; i++)
        {
            byte[] data = Encoding.UTF8.GetBytes($"Hello, World! {i}");
            int bytesSent = socket.Send(data);
        }

        byte[] recvBuff = new byte[1024];
        int recvBytes = socket.Receive(recvBuff);
        string recvString = Encoding.UTF8.GetString(recvBuff);

        Console.WriteLine($"[From Server] {recvString}");
        Thread.Sleep(500);
    }

}
catch (Exception e)
{
    Console.WriteLine(e);
}

