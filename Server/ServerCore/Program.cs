// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using System.Text;using ServerCore;


Listener _listener = new Listener();

try
{
    string host = Dns.GetHostName();
    IPHostEntry ipHostEntry = Dns.GetHostEntry(host);
    IPAddress ipAddress = ipHostEntry.AddressList[0];
    IPEndPoint endPoint = new IPEndPoint(ipAddress, 7777);
    
    try
    {
        _listener.Init(endPoint, () =>
        {
            return new GameSession();
        });
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

class GameSession : Session
{
    public override void OnConnected(EndPoint endpoint)
    {
        Console.WriteLine("OnConnected");
        byte[] data = Encoding.UTF8.GetBytes("Welcome to the server!");
        Send(data);
        Thread.Sleep(1000);

        Disconnect();
        
    }

    public override void OnRecv(ArraySegment<byte> data)
    {
        string recvData = Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);
        Console.WriteLine("[From Client]" + recvData);
    }

    public override void OnSend(int numOfBytes)
    {
        Console.WriteLine($"Transferred bytes: {numOfBytes}");

    }

    public override void OnDisconnected(EndPoint endpoint)
    {
        Console.WriteLine($"Disconnected from {endpoint}");
    }
}