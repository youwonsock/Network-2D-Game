using System;
using System.Net;
using System.Net.Sockets;

namespace ServerCore
{
    /// <summary>
    /// 클라이언트가 서버에 접속할 때 사용하는 클래스
    /// </summary>
    public class Connector
    {
        Func<PacketSession> sessionFactory;



        public void Connect(IPEndPoint endPoint, Func<PacketSession> sessionFactory, int count)
        {
            for (int i = 0; i < count; i++)
            {
                this.sessionFactory = sessionFactory;

                Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += OnConnectCompleted;
                args.RemoteEndPoint = endPoint;
                args.UserToken = socket;

                RegisterConnect(args);
            }
        }

        private void RegisterConnect(SocketAsyncEventArgs args)
        {
            Socket socket = args.UserToken as Socket;
            if (socket == null)
                return;

            bool pending = socket.ConnectAsync(args);
            if (pending == false)
                OnConnectCompleted(null, args);
        }

        private void OnConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                PacketSession session = sessionFactory.Invoke();

                session.Start(args.ConnectSocket);
                session.OnConnected(args.RemoteEndPoint);
            }
            else
            {
                Console.WriteLine($"OnConnectCompleted Fail: {args.SocketError}");
            }
        }
    }
}
