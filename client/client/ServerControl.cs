using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Net;
using System.Net.Sockets;
using Junhaehok;
using CF_Protocol;
using WebSocketSharp;

namespace client
{
    public delegate bool TrySendMSG(ushort command, byte[] data = null);
    public delegate void TryConnect();
    public class ServerControl
    {

        private WebSocket websock;
        private StringBuilder host = new StringBuilder();

        public string name;
        public TryConnect tryconnect;
        public TrySendMSG trysendmsg;
        public dgDisconnect Disconnect;             //client.cs
        public dgDataProcess DataAnalysis;          //client.cs
        public dgConnect CPconnect = null;

        private const int HEARTBEATINTERVAL = 30;
        private int heartbeat = 0;
        private Timer heartbeatTimer = new Timer();

        private const int CONNECTIONTIMEOUT = 5;
        private Timer connectionTimeout = new Timer();

        public Socket servsocket;
        public IPEndPoint myIPEP;

        private Packet sendPacket = new Packet();
        private const int HEADER_SIZE = HhhHelper.HEADER_SIZE;

        private SocketAsyncEventArgs sendEvent = new SocketAsyncEventArgs();
        private SocketAsyncEventArgs receiveHeaderEvent = new SocketAsyncEventArgs();



        public ServerControl(string ip, int port)  // calling at client.cs
        {
            if (port != 38080)
            {
                myIPEP = new IPEndPoint(IPAddress.Parse(ip), port);

                tryconnect = SockConnecting;
                trysendmsg = SendMSG;
            }
            else
            {
                host.Append("ws://");
                host.Append(ip);
                host.Append(":");
                host.Append(port.ToString());
                host.Append("/wsJinhyehok/");
                tryconnect = WebConnect;
                trysendmsg = WebSendMSG ;

            }
        }
        #region sock
        public void SockConnecting()    // calling at client.cs
        {
            //init socket
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //try connect
            SocketAsyncEventArgs serverConnectEvent = new SocketAsyncEventArgs();
            serverConnectEvent.RemoteEndPoint = myIPEP;
            serverConnectEvent.Completed += new EventHandler<SocketAsyncEventArgs>(Connect_Completed);
            s.ConnectAsync(serverConnectEvent);

            //connectionTimer
            connectionTimeout.Interval = CONNECTIONTIMEOUT * 1000;
            connectionTimeout.Elapsed += (sender, e) => { connectionTimeout.Stop(); Disconnection(); };
            connectionTimeout.Start();
        }

        private void Connect_Completed(object sender, SocketAsyncEventArgs e)
        {
            connectionTimeout.Stop();
            Socket connectSocket = (Socket)sender;
            servsocket = connectSocket;

            //connected
            if (true == connectSocket.Connected)
            {
                receiveHeaderEvent.UserToken = servsocket;
                //receive header
                receiveHeaderEvent.SetBuffer(new byte[HEADER_SIZE], 0, HEADER_SIZE);
                //receive Event
                receiveHeaderEvent.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveHeader_Completed);
                //request receive
                TryReceiveAsync(servsocket, receiveHeaderEvent);

                //heartbeat
                heartbeatTimer = new System.Timers.Timer();
                heartbeatTimer.Interval = HEARTBEATINTERVAL * 1000;
                heartbeatTimer.Elapsed += new System.Timers.ElapsedEventHandler(CheckHeartbeat);
                heartbeatTimer.Start();

                //connection passing
                CPconnect?.Invoke(this);
            }
            else
            {
                Disconnection();
            }
        }
        private void ReceiveHeader_Completed(object sender, SocketAsyncEventArgs e)
        {
            heartbeat = 0;
            Socket socketClient = (Socket)sender;

            //connected
            if (true == socketClient.Connected)
            {
                Packet receivePacket = new Packet();

                //header
                receivePacket.header = HhhHelper.BytesToHeader(e.Buffer);
                receivePacket.data = new byte[receivePacket.header.size];

                //body
                if (receivePacket.header.size <= 0)
                {
                    DataAnalysis(this,receivePacket);
                    TryReceiveAsync(socketClient, receiveHeaderEvent);
                }
                else
                {
                    SocketAsyncEventArgs receiveDataEvent = new SocketAsyncEventArgs();

                    receiveDataEvent.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveBody_Completed);
                    receiveDataEvent.SetBuffer(receivePacket.data, 0, receivePacket.header.size);
                    receiveDataEvent.UserToken = receivePacket;
                    TryReceiveAsync(socketClient, receiveDataEvent);
                }
            }
            else
            {
                Disconnection();
            }
        }
        private void ReceiveBody_Completed(object sender, SocketAsyncEventArgs e)
        {
            //data analysis
            DataAnalysis(this,(Packet)e.UserToken);

            //receive ready
            Socket client = (Socket)sender;
            TryReceiveAsync(client, receiveHeaderEvent);
        }
        private void CheckHeartbeat(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (++heartbeat > 3)
            {
                Disconnection();
                heartbeat = 0;
                return;
            }
            //SendMSG(HhhHelper.Code.HEARTBEAT);
        }
        public bool SendMSG(ushort command, byte[] data = null) // calling at client.cs
        {
            sendPacket = new Packet();

            if (servsocket == null)
                return false;

            sendPacket.header.code = command;
            sendPacket.header.uid=Client.uid;

            if (data == null)
            {
                sendPacket.data = new byte[0];
                sendPacket.header.size = (ushort)sendPacket.data.Length;
            }
            else
            {
                sendPacket.data = data;
                sendPacket.header.size = (ushort)sendPacket.data.Length;
            }

            byte[] szData = HhhHelper.PacketToBytes(sendPacket);
            sendEvent.SetBuffer(szData, 0, szData.Length);
            servsocket.SendAsync(sendEvent);

            return true;
        }
        public void Disconnection()         // calling at client.cs
        {
            //init socket
            if (servsocket != null ? servsocket.Connected : false)
            {
                servsocket.Close();
            }

            //heartbeat stop
            heartbeatTimer?.Stop();

            heartbeat = 0;
            servsocket = null;

            Disconnect(this);
        }
        private void TryReceiveAsync(Socket sock, SocketAsyncEventArgs sevent)
        {
            try
            {
                sock.ReceiveAsync(sevent);
                
            }
            catch (SocketException sock_e)
            {
                Console.WriteLine(sock_e.ToString());
                Disconnection();
            }
            catch (InvalidOperationException invaoper_e)
            {
                
            }

        }
        #endregion
        #region WEB
        private void WebConnect()
        {
            websock = new WebSocket(host.ToString());

            websock.OnOpen += WebOpenHandle;
            websock.OnMessage += WebMSGHandle;
            websock.OnClose += WebCloseHandle;
            websock.OnError += WebErrorHandle;

            websock.ConnectAsync();

            //connectionTimer
            connectionTimeout.Interval = CONNECTIONTIMEOUT * 1000;
            connectionTimeout.Elapsed += (sender, e) => { connectionTimeout.Stop(); Disconnection(); };
            connectionTimeout.Start();
        }
        private void WebOpenHandle(object sender, EventArgs e)
        {
            connectionTimeout.Stop();
            WebSocket connectSocket = (WebSocket)sender;
            websock = connectSocket;

            //connected
            if (true == connectSocket.IsAlive)
            { 
                //heartbeat
                heartbeatTimer = new System.Timers.Timer();
                heartbeatTimer.Interval = HEARTBEATINTERVAL * 1000;
                heartbeatTimer.Elapsed += new System.Timers.ElapsedEventHandler(CheckHeartbeat);
                heartbeatTimer.Start();

                //connection passing
                CPconnect?.Invoke(this);
            }
            else
            {
                WebDisconnection();                  
            }
        }
        private void WebErrorHandle(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.Exception);
            WebDisconnection();
        }
        private void WebCloseHandle(object sender, CloseEventArgs e)
        {
            WebDisconnection();
        }
        private void WebMSGHandle(object sender, MessageEventArgs e)
        {
            heartbeat = 0;
            WebSocket socketClient = (WebSocket)sender;

            //connected
            if (true == socketClient.IsAlive)
            {
                Packet receivepacket = HhhHelper.BytesToPacket(e.RawData);
                DataAnalysis(this, receivepacket);
            }
            else
            {
                WebDisconnection();
            }
        }
        private void WebDisconnection()
        {
            //init socket
            if (websock != null ? websock.IsAlive : false)
            {
                websock.CloseAsync();
            }

            //heartbeat stop
            heartbeatTimer?.Stop();

            heartbeat = 0;
            websock = null;

            Disconnect(this);
        }
        private bool WebSendMSG(ushort command, byte[] data = null)
        {
            sendPacket = new Packet();

            if (websock == null)
                return false;

            sendPacket.header.code = command;
            sendPacket.header.uid = Client.uid;

            if (data == null)
            {
                sendPacket.data = new byte[0];
                sendPacket.header.size = (ushort)sendPacket.data.Length;
            }
            else
            {
                sendPacket.data = data;
                sendPacket.header.size = (ushort)sendPacket.data.Length;
            }

            byte[] szData = HhhHelper.PacketToBytes(sendPacket);
            websock.SendAsync(szData, OnSendComplete);

            return true;
        }
        private static void OnSendComplete(bool t)
        {
            if (!t)
            {
                Console.WriteLine("WebSned Fail");
            }
        }
        #endregion
    }
}
