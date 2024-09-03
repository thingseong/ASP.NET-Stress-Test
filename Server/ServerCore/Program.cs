// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using System.Text;using ServerCore;

Console.WriteLine("Hello, World!");

Listener _listener = new Listener();

static void OnAcceptHandler(Socket clientSocket)
{
    try
    {
        Session session = new Session();
        session.Start(clientSocket);
        
        byte[] data = Encoding.UTF8.GetBytes("Welcome to the server!");
        session.Send(data);
        Thread.Sleep(1000);
        
        // session.Disconnect();
        session.Disconnect();
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
    
    // byte[] bytes = new byte[1024];
    // int recvBytes = clientSocket.Receive(bytes);
    // string recvData = Encoding.UTF8.GetString(bytes, 0, recvBytes);
    //
    // Console.WriteLine($"[From Client] {recvData}");
    // byte[] sendBytes = Encoding.UTF8.GetBytes("Welcome to the server!");
    // clientSocket.Send(sendBytes);
    //
    // clientSocket.Shutdown(SocketShutdown.Both);
    // clientSocket.Close();
}

try
{
    string host = Dns.GetHostName();
    Console.WriteLine($"Host: {host}");
    IPHostEntry ipHostEntry = Dns.GetHostEntry(host);
    IPAddress ipAddress = ipHostEntry.AddressList[0];
    IPEndPoint endPoint = new IPEndPoint(ipAddress, 7777);

    Console.WriteLine($"IP Address: {ipAddress}");
    Console.WriteLine($"IP Endpoint: {endPoint}");
    try
    {
        _listener.Init(endPoint, OnAcceptHandler);
    }
    catch(Exception e)
    {
        Console.WriteLine(e);
    }

    while (true)
    {
    }
}
catch (Exception e)
{
    Console.WriteLine(e);
}




