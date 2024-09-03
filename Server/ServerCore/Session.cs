using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore;

public abstract class Session
{
    Socket _socket;
    int disconnected = 0;
    
    object _lock = new object();
    Queue<byte[]> _sendQueue = new Queue<byte[]>();
    bool _pending = false;
    List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>(); 
    SocketAsyncEventArgs _snedArgs = new SocketAsyncEventArgs();
    // 재사용을 위해서 멤버변수로 줌

    public abstract void OnConnected(EndPoint endpoint);
    public abstract void OnRecv(ArraySegment<byte> data);
    public abstract void OnSend(int numOfBytes);
    public abstract void OnDisconnected(EndPoint endpoint);
    

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
        // 특정 데이터를 보낼거야
        lock (_lock)
        {
            // 근데 락을 걸어서 큐에는 한명씩만 접글할 수 있또록 하자.
            _sendQueue.Enqueue(sendBuff);
            if(_pendingList.Count == 0) RegisterSend(); // 내가 방금 보내야할 리스트를 다 보냈으면,
        }
    }

    public void Disconnect()
    {
        if (Interlocked.Exchange(ref disconnected, 1) == 1) return;
        OnDisconnected(_socket.RemoteEndPoint);
        _socket.Shutdown(SocketShutdown.Both);
        _socket.Close();
        
    }

    void RegisterSend()
    {
        // 펜딩리스트를 비웠으니까, 다시 펜딩 리스트를 채움
        while (_sendQueue.Count > 0)
        {
            byte[] sendBuff = _sendQueue.Dequeue();
            _pendingList.Add(new ArraySegment<byte>(sendBuff, 0, sendBuff.Length));
        }
        
        _snedArgs.BufferList = _pendingList;
        
        bool pending = _socket.SendAsync(_snedArgs); // 한번에 보내버림
        if(pending == false) OnSendCompleted(null, _snedArgs);
    }

    void OnSendCompleted(object sender, SocketAsyncEventArgs args)
    {
        lock (_lock)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) // 이 부분에서 동시성 이슈가 날 수 있음
            {
                try
                {
                    _snedArgs.BufferList = null;
                    _pendingList.Clear();
                    
                    OnSend(_snedArgs.BytesTransferred);
                    
                    if(_sendQueue.Count > 0) RegisterSend();
                    
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
                OnRecv(new ArraySegment<byte>(args.Buffer, args.Offset, args.BytesTransferred));

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