using System.Net;

using ServerCore;

namespace DummyClient
{
    class Client
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Client\n\n");

            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddress = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 7777);

            Connector connector = new Connector();
            connector.Connect(endPoint, () => { return new ServerSession(); });

            while (true)
            {


                Thread.Sleep(1000);
            }
        }
    }
}