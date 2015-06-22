
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Remoting.Activation;
using System.Text;
using System.Text.RegularExpressions;
using SR;
using SR.Server;

namespace ClientApp
{
    class Program
    {
        static void Main( string[] args )
        {
            Socket s = new Socket(AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);

            s.Connect("127.0.0.1",2222);

            Client client = new Client(s);

            client.Recieved += client_Recieved;
            client.Disconnected += client_Disconnected;

            while (true)
            {
                string text = Console.ReadLine();
              
                MemoryStream packet1 = SendTextMessage(text);
                MemoryStream packet2 = SendFileMessage( @"D:\Documents\Monthly Expenses.xls" , @"D:\Documents\test123.xls" );

                s.Send(packet1.ToArray());
                s.Send( packet2.ToArray() );
            }
        }

        static void client_Disconnected( Client sender )
        {
            
        }

        static void client_Recieved( Client sender , byte[] data )
        {
            Console.WriteLine(Encoding.Default.GetString(data));
        }


        private static MemoryStream SendTextMessage(string message)
        {
            // 1 Byte for Message Type
            // 4 Bytes for Message Length
            // 8 Bytes for broadcast group

            MemoryStream ms = new MemoryStream();

            byte[] typeofMessage = BitConverter.GetBytes( 1 );

            //1 Byte ofr Message Type
            ms.Write( typeofMessage , 0 , 1 );

            //4 Bytes for Message Length
            ms.Write( BitConverter.GetBytes( message.Length ) , 0 , 4 );

            //Write the message

            byte[] messageInBytes = Encoding.Default.GetBytes(message);

            ms.Write( messageInBytes , 0 , messageInBytes.Length );

            return ms;
        }

        private static MemoryStream SendFileMessage(string filePath, string remoteFilePath)
        {
            // 1 Byte for Message Type
            // 4 Bytes for remote file path Length
            //.... remote file path length
            //4 Bytes for file size to be transfered
            //.... file 

            MemoryStream ms = new MemoryStream();

            byte[] typeofMessage = BitConverter.GetBytes( 2 );

            //1 Byte ofr Message Type
            ms.Write( typeofMessage , 0 , 1 );

            FileStream fs = new FileStream( filePath , FileMode.Open , FileAccess.Read );

            byte[] pathInBytes = Encoding.Default.GetBytes( remoteFilePath );

            //Write  file path name length
            ms.Write( BitConverter.GetBytes( pathInBytes.Length ) , 0 , 4 );

            //write file path
            ms.Write( pathInBytes , 0 , pathInBytes.Length );

            //Write file size 
            ms.Write( BitConverter.GetBytes( fs.Length ) , 0 , 4 );

            //write the file content
            fs.CopyTo( ms );

            return ms;
        }

    }
}
