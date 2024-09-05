using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore;
public abstract class PacketSession : Session
{
    public static readonly int HeaderSize = 2;

    // [size(2)][packetId(2)][ ... ][size(2)][packetId(2)][ ... ]
    public sealed override int OnRecv(ArraySegment<byte> buffer)
    {
        int processLen = 0;

        while (true)
        {
            // 최소한 헤더는 파싱할 수 있는지 확인
            if (buffer.Count < HeaderSize)
                break;

            // 패킷이 완전체로 도착했는지 확인
            ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            if (buffer.Count < dataSize)
                break;

            // 여기까지 왔으면 패킷 조립 가능
            OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));
				
            processLen += dataSize;
            buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
        }

        return processLen;
    }

    public abstract void OnRecvPacket(ArraySegment<byte> buffer);
}

public abstract class Session
{
    RecvBuffer _recvBuffer = new RecvBuffer(1024);
    
    Socket _socket;
    int disconnected = 0;
    
    object _lock = new object();
    Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
    List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>(); 
    SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
    SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();
    // 재사용을 위해서 멤버변수로 줌

    public abstract void OnConnected(EndPoint endpoint);
    public abstract int OnRecv(ArraySegment<byte> data);
    public abstract void OnSend(int numOfBytes);
    public abstract void OnDisconnected(EndPoint endpoint);
    

    // 1. 연결된 Socket을 받아옴
    // 2. recvArgs를 이용해서 Receive 관련 비동기 콜백을 처리할 거임
    // 3. 그래서 Completed에 이벤트 추가
    // 4. 데이터를 받아올 버퍼 설정
    // 5. RegisterRecv를 이용해서 데이터 받기를 미리 걸어둠
    
    // 1. sendArgs를 이용해서 Send 관련 비동기 콜백 처리할 것
    // 2. 이벤트 추가 했음
    
    // * Callback 함수(이벤트)의 경우 Session이라는 Class를 상속 받아 사용하기 때문에
    // body는 상속 받는 Class에서 구현됨.
    
    public void Start(Socket socket)
    {
        _socket = socket;
        _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
        _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
        
        RegisterRecv(_recvArgs);
    }

    /// <summary>
    /// 1. OnConnected 에서 실행될 예정임
    /// 2. Lock을 걸어서 _sendQueue에는 한 쓰레드만 접근 할 수 있음
    /// 3. SendQueue는 내가 보내야할 데이터 들을 모아 둔 곳임. 왜냐하면, 비동기 처리라도 순차적으로 보내야해서 큐를 이용해서 쌓아둠.
    /// 
    /// 
    /// </summary>
    /// <param name="sendBuff"></param>
    public void Send(ArraySegment<byte> sendBuff)
    {
        // 특정 데이터를 보낼거야
        lock (_lock)
        {
            // 근데 락을 걸어서 큐에는 한명씩만 접글할 수 있또록 하자.
            _sendQueue.Enqueue(sendBuff);
            if(_pendingList.Count == 0) RegisterSend(); // 내가 방금 보내야할 리스트를 다 보냈으면,
        }
    }
    
    /// <summary>
    /// 1. Disconnect시 disconnected변수를 설정할 때 원자성을 가져야함.
    /// </summary>
    public void Disconnect()
    {
        if (Interlocked.Exchange(ref disconnected, 1) == 1) return;
        OnDisconnected(_socket.RemoteEndPoint);
        _socket.Shutdown(SocketShutdown.Both);
        _socket.Close();
        
    }

    /// <summary>
    /// 1. 세션마다 SendQueue와 pendingList가 있음
    /// 2. 현재 세션에 존재하는 queue에 있는 데이터를 pendingList에 다 넣음.
    /// 3. sendArgs(SocketAsyncEventArgs)에 BufferList를 설정해줌
    /// 4. 이를 기반으로 한번에 데이터를 보내버림
    /// 5. 이후 pending에 따라 다르게 처리
    /// </summary>
    void RegisterSend()
    {
        // 펜딩리스트를 비웠으니까, 다시 펜딩 리스트를 채움
        while (_sendQueue.Count > 0)
        {
            ArraySegment<byte> buff = _sendQueue.Dequeue();
            _pendingList.Add(buff);
        }
        
        _sendArgs.BufferList = _pendingList;
        
        bool pending = _socket.SendAsync(_sendArgs); // 한번에 보내버림
        if(pending == false) OnSendCompleted(null, _sendArgs);
    }
    
    /// <summary>
    /// 1. Send가 완료 됐을 때
    /// 2. Lock을 하는 이유는 보내는 일은 순차적으로 처리해야해서, 동시에 작업하다보면 펜딩리스트에 문제가 생길수 있음
    /// 3. 펜딩리스트를 아직 사용 안했는데 비워질 수 도 있음 (동시성 이슈)
    /// 4. 큐가 아직 안비어 있다면 다시 Send를 해준다.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void OnSendCompleted(object sender, SocketAsyncEventArgs args)
    {
        lock (_lock)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) // 이 부분에서 동시성 이슈가 날 수 있음
            {
                try
                {
                    _sendArgs.BufferList = null;
                    _pendingList.Clear();
                    
                    OnSend(_sendArgs.BytesTransferred);
                    
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

    // 1. ReceivedAsync를 걸어둠
    // 2. pending에 따라 처림
    void RegisterRecv(SocketAsyncEventArgs args)
    {
        _recvBuffer.Clean();
        ArraySegment<byte> segment = _recvBuffer.WriteSegment;
        _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);
        
        bool pending = _socket.ReceiveAsync(args);
        if(pending == false) OnRecvCompleted(null, args);
    }
    
    /// <summary>
    /// 1. 데이터 길이가 0보다 길고 성공했을 때
    /// 2. OnRecv(콜백, 이벤트)를 이용해서 Recv를 처리함
    /// 3. 그리고 다시 Receive를 등록함.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
    {
        if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
        {
            try
            {
                // Write 커서 이동
                if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
                {
                    Disconnect();
                    return;
                }

                // 컨텐츠 쪽으로 데이터를 넘겨주고 얼마나 처리했는지 받는다
                int processLen = OnRecv(_recvBuffer.ReadSegment);
                if (processLen < 0 || _recvBuffer.DataSize < processLen)
                {
                    Disconnect();
                    return;
                }

                // Read 커서 이동
                if (_recvBuffer.OnRead(processLen) == false)
                {
                    Disconnect();
                    return;
                }

                RegisterRecv(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        else
        {
            Disconnect();
        }
    }
}