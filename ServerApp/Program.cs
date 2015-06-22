using System;
using SR.Server;

namespace ServerApp
{
    class Program
    {
        static void Main( string[] args )
        {
           Srv.Start(2222);

            Console.Read();
        }

    }
}
