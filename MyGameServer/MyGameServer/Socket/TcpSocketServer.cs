using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MyGameServer.Socket;

public class TcpSocketServer
{
    private readonly int _port;
    private TcpListener _listener;

    public TcpSocketServer(int port)
    {
        _port = port;
    }

    public async Task StartAsync()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        Console.WriteLine($"TCP Socket Server is listening on port {_port}");

        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();
            _ = ProcessClientAsync(client);
        }
        
    }

    public async Task ProcessClientAsync(TcpClient client)
    {
        using (var networkStream = client.GetStream())
        {
            var buffer = new byte[1024];
            int bytesRead;

            while ((bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                var request = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                Console.WriteLine($"Received {request}");
                
                var response = Encoding.ASCII.GetBytes("Echo : " + request);
                
                await networkStream.WriteAsync(response, 0, response.Length);

                
            }
        }
        client.Close();
        Console.WriteLine($"Client disconnected");
        
    }
    
    
}