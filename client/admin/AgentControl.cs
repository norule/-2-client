using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Net;
using System.Net.Sockets;
using Junhaehok;

namespace Admin
{
    public class AgentControl
    {
        public string type;
        public dgDataProcess DataAnalysis;
        public dgDisconnection removeThisConnectioinlist;
        public dgOnconnection removeThisWaitList;
        private const int HEARTBEATINTERVAL = 3;
        private int heartbeat = 0;
        private Timer heartbeatTimer = new Timer();

        private const int CONNECTIONTIMEOUT = 5;
        private Timer connectionTimeout = new Timer();

        public Socket adminSock;
        public IPEndPoint myIPEP;

        private Packet sendPacket = new Packet();
        private const int HEADER_SIZE = HhhHelper.HEADER_SIZE;

        private SocketAsyncEventArgs sendEvent = new SocketAsyncEventArgs();
        private SocketAsyncEventArgs receiveHeaderEvent = new SocketAsyncEventArgs();

        public int roomCount;
        public int userCount;
        public bool alive;

        public AgentControl(string _type,IPEndPoint inputIPEP)
        {
            type = _type;
            myIPEP = inputIPEP;
            adminSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
            adminSock = connectSocket;

            //connected
            if (true == connectSocket.Connected)
            {
                removeThisWaitList(this);                   //list set

                receiveHeaderEvent.UserToken = adminSock;
                //receive header
                receiveHeaderEvent.SetBuffer(new byte[HEADER_SIZE], 0, HEADER_SIZE);
                //receive Event
                receiveHeaderEvent.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveHeader_Completed);
                //request receive
                TryReceiveAsync(adminSock, receiveHeaderEvent);

                //heartbeat
                heartbeatTimer = new System.Timers.Timer();
                heartbeatTimer.Interval = HEARTBEATINTERVAL * 1000;
                heartbeatTimer.Elapsed += new System.Timers.ElapsedEventHandler(CheckHeartbeat);
                heartbeatTimer.Start();
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
        }
        public bool SendMSG(ushort command, byte[] data = null)
        {
            sendPacket = new Packet();

            if (adminSock == null)
                return false;

            sendPacket.header.code = command;
            sendPacket.header.uid = 0;

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

            try
            {
                adminSock.SendAsync(sendEvent);
            }
            catch (SocketException e)
            {
                Disconnection();
            }

            return true;
        }

        public void Disconnection()
        {
            //init socket
            if (adminSock != null ? adminSock.Connected : false)
            {
                adminSock.Close();
            }

            //heartbeat stop
            heartbeatTimer?.Stop();

            heartbeat = 0;
            adminSock = null;

            removeThisConnectioinlist(this);        //list set
        }
        private void TryReceiveAsync(Socket sock, SocketAsyncEventArgs sevent)
        {
            try
            {
                sock.ReceiveAsync(sevent);
            }
            catch (Exception e)
            {
                Disconnection();
                
            }
        }
    }
}
