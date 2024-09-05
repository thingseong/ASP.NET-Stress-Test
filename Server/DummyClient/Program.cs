// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using System.Text;
using ServerCore;

Console.WriteLine("Hello, World!");

string host = Dns.GetHostName();
Console.WriteLine($"Host: {host}");
IPHostEntry ipHostEntry = Dns.GetHostEntry(host);
IPAddress ipAddress = ipHostEntry.AddressList[0];
IPEndPoint endPoint = new IPEndPoint(ipAddress, 7777);

Connector connector = new Connector();
connector.Connect(endPoint, () =>
{
    return new GameSession();
});

// 1. Scoket에 연결
// 2. 요청 5번 보냄 (블록킹)
// 3. 그 이후에 Receive를 통해서 기다림
// 4. 0.5초 기다림

while (true)
{
    try
    {
        
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
}

class Packet
{
    public ushort size;
    public ushort packetId;
}


class GameSession : Session 
{
    public override void OnConnected(EndPoint endpoint)
    {
        Console.WriteLine("OnConnected");
        Packet packet = new Packet() { size = 4, packetId = 7 };
        
        for (int i = 0; i < 5; i++)
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
            byte[] buffer = BitConverter.GetBytes(packet.size);
            byte[] buffer2 = BitConverter.GetBytes(packet.packetId);
            Array.Copy(buffer, 0 , openSegment.Array, openSegment.Offset, buffer.Length);
            Array.Copy(buffer2, 0 , openSegment.Array, openSegment.Offset + buffer.Length, buffer2.Length);
        
            ArraySegment<byte> sendBuff =SendBufferHelper.Close(packet.size);
        
            Send(sendBuff);
        }
        
    }

    public override int OnRecv(ArraySegment<byte> data)
    {
        string recvData = Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);
        Console.WriteLine("[From Server]" + recvData);
        return data.Count;
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

