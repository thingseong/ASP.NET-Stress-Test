using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;

namespace MyGameServer.Socket;

public class TcpSocketServer
{
    private readonly int _port;
    private TcpListener _listener;
    // private readonly ConcurrentDictionary<string, byte[]> _cache = new ConcurrentDictionary<string, byte[]>();
    private readonly IDistributedCache _cache;
    
    public TcpSocketServer(int port, IDistributedCache cache)
    {
        _port = port;
        _cache = cache;
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

                var cachedResponse = await _cache.GetStringAsync(request);
                
                if (!string.IsNullOrEmpty(cachedResponse))
                {
                    Console.WriteLine("Cache hit. Returning cached response.");
                    var cachedResponseBytes = Encoding.ASCII.GetBytes(cachedResponse);
                    await networkStream.WriteAsync(cachedResponseBytes,  0, cachedResponseBytes.Length);
                }
                else{
                    
                    for(int i = 0; i < 100000000; i++){}
                    
                    var response = Encoding.ASCII.GetBytes("Echo : " + request + '\n');
                    var cacheReponse ="Cached Echo : " + request +'\n';
                    
                    // _cache[request] = cacheReponse;
                    await _cache.SetStringAsync(request, cacheReponse);
                    await networkStream.WriteAsync(response, 0, response.Length);
                }

                //break;
            }
        }
        client.Close();
        Console.WriteLine($"Client disconnected");
        
    }
    
    
}