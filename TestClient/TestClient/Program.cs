using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class TcpClientApp
{
    private readonly string _host;
    private readonly int _port;

    public TcpClientApp(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public async Task ConnectAndSendAsync()
    {
        using (var client = new TcpClient())
        {
            try
            {
                // 서버에 연결합니다.
                await client.ConnectAsync(_host, _port);
                Console.WriteLine("Connected to server.");

                using (var stream = client.GetStream())
                {
                    while (true)
                    {
                        // 사용자로부터 메시지를 입력받습니다.
                        Console.Write("Enter message to send (or type 'exit' to quit): ");
                        var message = Console.ReadLine() + "\r\n";

                        if (message.Equals("exit", StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }

                        var data = Encoding.ASCII.GetBytes(message);
                        await stream.WriteAsync(data, 0, data.Length);
                        Console.WriteLine($"Sent: {message.Trim()}");

                        // 서버로부터 응답을 받습니다.
                        var buffer = new byte[1024];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        var response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"Received from server: {response}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }

    static async Task Main(string[] args)
    {
        var client = new TcpClientApp("127.0.0.1", 9000); // 서버 IP와 포트 설정
        await client.ConnectAndSendAsync();
    }
}
