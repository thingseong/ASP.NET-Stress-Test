using System.Net;
using System.Net.Sockets;

namespace ServerCore;

public class Listener
{
    Socket _listenSocket;
    
    // Func의 경우 특정 타입을 리턴함 아래의 경우 Session
    Func<Session> _sessionFactory;

    // 1. Socket을 열어줌 with TCP
    // 2. Main에서 받아온 sessionFactory 라는 Func을 넣어줌
    // 3. SocketAsyncEventArgs를 이용함 왜냐하면, AccpetAsync를 사용하려면 필요함
    // 4. 특정 비동기가 완료 되었을 때(args.Completed) 실행할 함수를 추가해줘야함.
    // 5. 그리고 일단 AcceptAsync를 등록하기 위해 ReigsterAccept를 실행함.
    public void Init(IPEndPoint endPoint, Func<Session> sessionFactory)
    {
        
        _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _sessionFactory += sessionFactory;
        
        _listenSocket.Bind(endPoint);
        _listenSocket.Listen(100);
        
        SocketAsyncEventArgs args = new SocketAsyncEventArgs();
        args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
       
        RegisterAccept(args);
        
        

    }

    // 1. 우선 AcceptSocket, 이전에 사용했던 소켓을 비워줌
    // 2. 비동기로 Accept를 실행 시킴
    // 3. pending(보류)가 안됐으면, 실행하자마자 바로 처리했으면 바로 OnAcceptCompleted를 호출함 SocketAsyncEventArgs 를 바탕으로
    void RegisterAccept(SocketAsyncEventArgs args)
    {
        args.AcceptSocket = null; // args로 Accept를 하기전에 초기화 해줘야함.
        bool pending = _listenSocket.AcceptAsync(args);
        if (pending == false)
            OnAcceptCompleted(null, args);
        
    }

    // 0. 호출 되는 경우 :
    //      a. SocketAsyncEventArgs에 등록되어서 AcceptAsync가 완료 된 경우
    //      b. pending == false 라서 바로 실행 해야할 경우
    // 1. Socket 에러 체크
    // 2. sessionFactory(Func)를 이용하여 하나의 세션을 생성해줌
    // 3. 세션을 Start 함 내가 받아온 소켓을 기반으로
    // 4. 연결되었으니 OnConnected를 호출함.
    void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
    {
        if (args.SocketError == SocketError.Success)
        {
            Session session = _sessionFactory.Invoke();
            session.Start(args.AcceptSocket);
            session.OnConnected(args.AcceptSocket.RemoteEndPoint);
            // TODO
            //_onAcceptHandler.Invoke(args.AcceptSocket);
        }
        else
            Console.WriteLine("Socket Error: " + args.SocketError);
        RegisterAccept(args);
    }
    
}