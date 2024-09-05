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


class Packet
{
    public ushort size;
    public ushort packetId;
}

class LoginOkPacket : Packet
{
    
}


class GameSession : PacketSession
{
    public override void OnConnected(EndPoint endpoint)
    {
        
        Console.WriteLine("OnConnected");

        // Packet packet = new Packet() { size = 4, packetId = 7 };
        //
        // ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
        // byte[] buffer = BitConverter.GetBytes(packet.size);
        // byte[] buffer2 = BitConverter.GetBytes(packet.packetId);
        // Array.Copy(buffer, 0 , openSegment.Array, openSegment.Offset, buffer.Length);
        // Array.Copy(buffer2, 0 , openSegment.Array, openSegment.Offset + buffer.Length, buffer2.Length);
        //
        // ArraySegment<byte> sendBuff =SendBufferHelper.Close(buffer.Length + buffer2.Length);
        //
        // Send(sendBuff);
        Thread.Sleep(1000);

        Disconnect();
        
    }
    


    public override void OnSend(int numOfBytes)
    {
        Console.WriteLine($"Transferred bytes: {numOfBytes}");

    }

    public override void OnDisconnected(EndPoint endpoint)
    {
        Console.WriteLine($"Disconnected from {endpoint}");
    }

    public override void OnRecvPacket(ArraySegment<byte> buffer)
    {
        ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
        ushort packetId = BitConverter.ToUInt16(buffer.Array, buffer.Offset + 2);
        Console.WriteLine($"Received packet size: {size} packet id: {packetId}");
    }
}