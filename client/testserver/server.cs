using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Junhaehok;
namespace testserver
{
    class server
    {

        public static byte[] szData;
        public static Socket m_ServerSocket;
        static void Main(string[] args)
        {


            m_ServerSocket = new Socket(
                               AddressFamily.InterNetwork,
                               SocketType.Stream,
                               ProtocolType.Tcp);
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 11000);
            m_ServerSocket.Bind(ipep);
            m_ServerSocket.Listen(20);

            SocketAsyncEventArgs ae = new SocketAsyncEventArgs();
            ae.Completed
                += new EventHandler<SocketAsyncEventArgs>(Accept_Completed);
            m_ServerSocket.AcceptAsync(ae);

            Console.ReadLine();
        }

        public static void Accept_Completed(object sender, SocketAsyncEventArgs e)
        {
            Socket ClientSocket = e.AcceptSocket;


            if (ClientSocket != null)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                szData = new byte[1024];
                args.SetBuffer(szData, 0, 1024);
                args.UserToken = ClientSocket;
                args.Completed
                    += new EventHandler<SocketAsyncEventArgs>(Receive_Completed);
                ClientSocket.ReceiveAsync(args);
            }
            e.AcceptSocket = null;
            m_ServerSocket.AcceptAsync(e);
        }

        public static void Receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            Socket ClientSocket = (Socket)sender;
            if (ClientSocket.Connected && e.BytesTransferred > 0)
            {

                Packet receiveProtocol = HhhHelper.BytesToPacket(e.Buffer);

                Console.WriteLine("t"+receiveProtocol.header.code);
                Console.WriteLine(receiveProtocol.header.size);
                Console.WriteLine(Encoding.UTF8.GetString(receiveProtocol.data,0,receiveProtocol.header.size));

                e.SetBuffer(szData, 0, 1024);

                SocketAsyncEventArgs sendEvent = new SocketAsyncEventArgs();


                if (receiveProtocol.header.code == HhhHelper.Code.HEARTBEAT)
                    receiveProtocol.header.code = HhhHelper.Code.HEARTBEAT_SUCCESS;
                else if (receiveProtocol.header.code == HhhHelper.Code.SIGNIN)
                    receiveProtocol.header.code = HhhHelper.Code.SIGNIN_SUCCESS;

                byte[] szData2 = HhhHelper.PacketToBytes(receiveProtocol);
                sendEvent.SetBuffer(szData2, 0, szData2.Length);


                ClientSocket.SendAsync(sendEvent);
                ClientSocket.ReceiveAsync(e);
            }
            else
            {
                ClientSocket.Disconnect(false);
                ClientSocket.Dispose();
            }
        }

    }
}
