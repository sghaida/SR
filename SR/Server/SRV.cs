using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace SR.Server
{
    public sealed class Srv
    {
        private static Listener _srvCore;
        private static List<Socket> _sockets = new List<Socket>();


        public static void Start(int port)
        {
            _srvCore = new Listener(port);
            _srvCore.SocketAccepted += srvCore_SocketAccepted;
            _srvCore.Start();
        }

        private static void srvCore_SocketAccepted( Socket e )
        {
            _sockets.Add( e );

            Console.WriteLine( "New Connection: {0}\n{1}\n==========" , e.RemoteEndPoint , DateTime.Now );

            Client client = new Client( e );
            client.Recieved += client_Recieved;
            client.Disconnected += client_Disconnected;

        }

        static void client_Disconnected( Client sender )
        {
            
        }

        static void client_Recieved( Client sender , byte[] data )
        {
            Console.WriteLine(sender.Id);
            Console.WriteLine( Encoding.Default.GetString( data ) );
            Console.WriteLine( "==============");
        }

        public static List<Socket> GetSockList()
        {
            return _sockets;
        }


    }
}
