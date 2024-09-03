using System.Net.Sockets;
using System.Text;

namespace ServerCore;

public class Session
{
    Socket _socket;
    int disconnected = 0;
    
    object _lock = new object();
    Queue<byte[]> _sendQueue = new Queue<byte[]>();
    bool _pending = false;
    SocketAsyncEventArgs _snedArgs = new SocketAsyncEventArgs();
    public void Start(Socket socket)
    {
        _socket = socket;
        SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();
        recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
        recvArgs.SetBuffer(new byte[1024], 0, 1024);
        
        
        _snedArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
        
        RegisterRecv(recvArgs);
    }

    public void Send(byte[] sendBuff)
    {
        lock (_lock)
        {
            _sendQueue.Enqueue(sendBuff);
            if(_pending == false) RegisterSend();
        }
    }

    public void Disconnect()
    {
        if (Interlocked.Exchange(ref disconnected, 1) == 1) return;
        _socket.Shutdown(SocketShutdown.Both);
        _socket.Close();
    }

    void RegisterSend()
    {
        _pending = true;
        byte[] buffer = _sendQueue.Dequeue();
        _snedArgs.SetBuffer(buffer, 0, buffer.Length);
        
        bool pending = _socket.SendAsync(_snedArgs);
        if(pending == false) OnSendCompleted(null, _snedArgs);
    }

    void OnSendCompleted(object sender, SocketAsyncEventArgs args)
    {
        lock (_lock)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    if(_sendQueue.Count > 0) RegisterSend();
                    else _pending = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            else
            {
                // TODO Disconnect
                Disconnect();
            }
        }
    }

    void RegisterRecv(SocketAsyncEventArgs args)
    {
        bool pending = _socket.ReceiveAsync(args);
        if(pending == false) OnRecvCompleted(null, args);
    }

    void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
    {
        if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
        {
            try
            {
                string recvData = Encoding.UTF8.GetString(args.Buffer, args.Offset, args.BytesTransferred);
                Console.WriteLine("[From Client]" + recvData);
                RegisterRecv(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        else
        {
            // TODO Disconnect
            Disconnect();
        }
    }
}