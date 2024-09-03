using System.Net;
using System.Net.Sockets;

namespace ServerCore;

public class Listener
{
    Socket _listenSocket;
    private Action<Socket> _onAcceptHandler;

    public void Init(IPEndPoint endPoint, Action<Socket> onAcceptHandler)
    {
        _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _onAcceptHandler = onAcceptHandler;
        
        _listenSocket.Bind(endPoint);

        _listenSocket.Listen(100);
        
        SocketAsyncEventArgs args = new SocketAsyncEventArgs();
        args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
        RegisterAccept(args);

    }

    void RegisterAccept(SocketAsyncEventArgs args)
    {
        args.AcceptSocket = null; // args로 Accept를 하기전에 초기화 해줘야함.
        bool pending = _listenSocket.AcceptAsync(args);
        if (pending == false)
            OnAcceptCompleted(null, args);
        
    }

    void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
    {
        if (args.SocketError == SocketError.Success)
        {
            // TODO
            _onAcceptHandler.Invoke(args.AcceptSocket);
        }
        else
            Console.WriteLine("Socket Error: " + args.SocketError);
        RegisterAccept(args);
    }
    
}