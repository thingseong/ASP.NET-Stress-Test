// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using System.Text;
using ServerCore;


Listener _listener = new Listener(); // 리스너를 이용해서 연결 요청을 받을 거임.
Console.WriteLine("Starting server...");

string host = Dns.GetHostName();
IPHostEntry ipHostEntry = Dns.GetHostEntry(host);
IPAddress ipAddress = ipHostEntry.AddressList[0];
IPEndPoint endPoint = new IPEndPoint(ipAddress, 7777);
// 1. 주소 받아옴

try
{
    // 특정 주소(endPoint)를 열어줌
    // 그리고 열렸을 때 어떤 함수를 실행할 지 정함.
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