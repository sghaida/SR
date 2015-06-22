

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SR.Server
{
    public sealed class Client
    {
        public string Id { get; private set; }

        public IPEndPoint EndPoint { get; private set; }

        private Socket _sock;

        public Client(Socket accepted,string broadcastGroup=null)
        {
            _sock = accepted;
            
            Id = Guid.NewGuid().ToString();
            
            EndPoint = (IPEndPoint) _sock.RemoteEndPoint;
            
            _sock.BeginReceive(new byte[]{0}, 0, 0, 0, CallBack, null);
        }

        void CallBack(IAsyncResult ar)
        {
            try
            {
                _sock.EndReceive(ar);

                byte[] messageTypeBuffer = new byte[1];
               
                //Get Message type
                int rec = _sock.Receive(messageTypeBuffer, messageTypeBuffer.Length, 0);       
                
                //This is a normal text
                int messageType = Convert.ToInt32(messageTypeBuffer[0]);

                if ( messageType == (int)Helpers.MessageType.TXT )
                {
                    byte[] dataLengthBuffer = new byte[ 4 ];

                    //getMessage Size or file Path name size
                    rec = _sock.Receive( dataLengthBuffer , dataLengthBuffer.Length , 0 );

                    //Get the length of the data that will be transfered 
                    int length = BitConverter.ToInt32( dataLengthBuffer , 0 );

                    //Check if the data should be written to memory stream or not
                    if ( length <= 2048 )
                    {
                       byte[] data = ReadShort(ref _sock, length);
                      
                        if ( Recieved != null )
                           Recieved( this , data );
                    }
                    else
                    {
                        MemoryStream ms = new MemoryStream();

                        ms.SetLength( length );
                        ms.Seek( 0 , SeekOrigin.Begin );
                        ms.Flush();

                        ms = ReadLong(ref _sock, length);

                        ms.Close();
                        ms.Dispose();

                        if ( Recieved != null )
                            Recieved( this , ms.ToArray() );
                       
                    }
                }
                else if ( messageType == (int)Helpers.MessageType.FLE )
                {
                    byte[] filePathNameSizeBuffer = new byte[4];
                    byte[] fileSizeBuffer = new byte[ 4 ];
                    byte[] filePathBuffer;

                    string filePath = string.Empty;

                    MemoryStream ms = new MemoryStream();
                    
                    //Read filePathNameSize in bytes 
                    filePathNameSizeBuffer = ReadShort(ref _sock, 4);

                    int filePathlength = BitConverter.ToInt32( filePathNameSizeBuffer , 0 );

                    //Read filePath

                    if (filePathlength <= 2048)
                    {

                        filePathBuffer = ReadShort(ref _sock, filePathlength);

                        filePath = Encoding.Default.GetString( filePathBuffer );
                    }
                    else
                    {
                        ms.SetLength( filePathlength );
                        ms.Seek( 0 , SeekOrigin.Begin );
                        ms.Flush();

                        ms = ReadLong( ref _sock , filePathlength );
                        
                        filePath = Encoding.Default.GetString(ms.ToArray());
                    }

                    //Create or override file

                    if (DirectoryExists(filePath))
                    {
                        FileStream fs = new FileStream( filePath , FileMode.OpenOrCreate , FileAccess.Write );

                        //read File size
                        fileSizeBuffer = ReadShort( ref _sock , 4 );

                        int fileSize = BitConverter.ToInt32( fileSizeBuffer , 0 );

                        //rewind to the begining of the memory stream
                        ms.Seek( 0 , SeekOrigin.Begin );

                        //Readfile to memory stream
                        ms = ReadLong( ref _sock , fileSize );

                        //rewind to the begining of the memory stream
                        ms.Seek(0, SeekOrigin.Begin);
                        ms.Flush();

                        ms.CopyTo( fs );

                        fs.Flush();
                        fs.Close();
                        fs.Dispose();

                        ms.Close();
                        ms.Dispose();

                        if (Recieved != null)
                            Recieved(this,
                                Encoding.Default.GetBytes(string.Format("File {0} : has been recived ", filePath)));
                    }

                   
                }
                else if (BitConverter.ToInt32(messageTypeBuffer, 0) == (int) Helpers.MessageType.OBJ)
                {
                    //Message is an object
                }
              
                //if (rec < buffer.Length)
                //{
                //    Array.Resize<byte>(ref buffer,rec);
                //}

                _sock.BeginReceive( new byte[] { 0 } , 0 , 0 , 0 , CallBack , null );

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Close();

                if (Disconnected != null)
                {
                    Disconnected(this);
                }

            }
        }

        public void Close()
        {
            _sock.Close();
            _sock.Dispose();
        }

        private Byte[] ReadShort( ref Socket s , int length )
        {
            byte[] buffer = new byte[ length ];

            int rec = s.Receive( buffer , length , 0 );

            //if (Recieved != null)
            //    Recieved(this, buffer);
            
            return buffer;
        }

        private MemoryStream ReadLong( ref Socket s , int length )
        {
            byte[] buffer = new byte[2048];

            MemoryStream ms = new MemoryStream();

            ms.SetLength( length );
            ms.Seek( 0 , SeekOrigin.Begin );
            ms.Flush();

            int remainingLength = length;

            //Data should be written to memory stream
            while ( true )
            {
                int numberOfByteRead = s.Receive( buffer , 0 , buffer.Length , 0 );

                remainingLength -= numberOfByteRead;

                if ( numberOfByteRead < buffer.Length )
                {
                    Array.Resize<byte>( ref buffer , numberOfByteRead );
                }

                ms.Write( buffer , 0 , buffer.Length );

                if ( remainingLength == 0 )
                {
                    break;
                }
            }

            //if ( Recieved != null )
            //{   
            //    Recieved( this , ms.ToArray() );
            //    return ms;
            //}

            return ms;
        }

        private bool DirectoryExists(string filePath)
        {
            string path = Path.GetDirectoryName(filePath);

            return ( Directory.Exists( path ));
        }

        public delegate void ClientReceivedHandler( Client sender , byte[] data );

        public delegate void ClientDisconnectedHandler(Client sender);

        public event ClientReceivedHandler Recieved;
        public event ClientDisconnectedHandler Disconnected;

    }
}
