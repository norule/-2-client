using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Net;
using System.Net.Sockets;
using Junhaehok;
using CF_Protocol;

namespace client
{
    public class ServerControl
    {
        public string name;
        public dgDisconnect Disconnect;//본체용
        public dgDataProcess DataAnalysis;
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
        //private SocketAsyncEventArgs receiveDataEvent = new SocketAsyncEventArgs();

        public ServerControl(string ip, int port)
        {
            myIPEP = new IPEndPoint(IPAddress.Parse(ip), port);
            servsocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Connecting()
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
                Console.WriteLine("*** Connected ***");

                //heartbeat
                heartbeatTimer = new System.Timers.Timer();
                heartbeatTimer.Interval = HEARTBEATINTERVAL * 1000;
                heartbeatTimer.Elapsed += new System.Timers.ElapsedEventHandler(CheckHeartbeat);
                heartbeatTimer.Start();
                //SendMSG(HhhHelper.Code.HEARTBEAT);

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
        public bool SendMSG(ushort command, byte[] data = null)
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

        public void Disconnection()
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
            catch (SocketException e)
            {
                Console.WriteLine(e.ToString());
                
            }
            catch (InvalidOperationException e2)
            {
                
            }

        }
    }
}
