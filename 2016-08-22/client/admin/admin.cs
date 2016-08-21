using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Admin
{
    class Admin
    {
        /*
        private Socket listenSock;                //accept socket
        private IPEndPoint myIPEP;              //myIPEP
        private static  AdminState myState;

        private List<ServerHandle> servList = new List<ServerHandle>();
        private List<UserHandle> userList = new List<UserHandle>();

        public struct ServerHandle
        {
            public Socket sock;
            public string IP;
            public int roomCount;
            public int userCount;

            public ServerHandle(Socket s, String ip=null ,int _roomcount=0, int _usercount=0)
            {
                sock = s;
                IP = ip;
                roomCount = _roomcount;
                userCount = _usercount;
            }
        }
        //agent.IP = (clientSock.RemoteEndPoint as IPEndPoint).Address.ToString();
        //agent.roomCount;
          //          agent.userCount;

        public struct UserHandle
        {
            public int Rank;
            public string ID;
            public int MSGCOUNT;
            public UserHandle(int r, string i, int mc)
            {
                Rank = r;
                ID = i;
                MSGCOUNT = mc;
            }
        }

        enum AdminState
        {
            None = 0,
            Monitor,
            ServerState,
            UserINFO
        }


        */
        static void Main(string[] args)
        {
            //Admin admin = new Admin();
            //admin.Start();
        }

        /*
        public void Start()
        {
            Init("127.0.0.1", 98765);

            while (true)                                                               //Accpet 
            {
                try
                {
                    Socket clientSock = listenSock.Accept();

                    ServerHandle agent = new ServerHandle();

                    agent.sock = clientSock;
                    
                    servList.Add(agent);

                    clientSock = null;
                }
                catch (SocketException e)
                {
                }
                catch (Exception e)
                {
                }
            }

            Process();
        }
        private void Init(String IP, int port)
        {
            myIPEP = new IPEndPoint(IPAddress.Parse(IP), port);
            listenSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSock.Bind(myIPEP);
            listenSock.Listen(5);
        }

        private void Process()
        {
            while (true)
            {
                Console.Clear();
                switch (myState)
                {
                    #region case AdminState.None:
                    case AdminState.None:
                        Console.WriteLine("wait connect");
                        Thread.Sleep(100);
                        break;
                    #endregion

                    #region case AdminState.Rank:
                    case AdminState.UserINFO:

                        Console.WriteLine("+----------------------------------------+");
                        Console.WriteLine("|  {0,5} |   {1,12} |   {2,10} |", "Rnak", "ID", "Count");
                        Console.WriteLine("+----------------------------------------+");

                        for (int i = 0; i < userList.Count; i++)
                        {
                            Console.WriteLine("|   {0,4} |   {1,12} |   {2,10} |", i+1, userList[i].ID, userList[i].MSGCOUNT);
                        }

                        Console.WriteLine("+----------------------------------------+");

                        Console.WriteLine("##   1. userstate     2. serverstate    ##"); //commands
                        Console.WriteLine("##   3. deleteUser                      ##\n\n"); //commands


                        while (true)
                        {
                            string userControlMSG;

                            Console.Write(">>>");
                            userControlMSG = Console.ReadLine();

                            if (UserCommandProcess(userControlMSG))
                            {
                                break;
                            }
                        }
                        break;
                    #endregion

                    #region case AdminState.ServerState:
                    case AdminState.ServerState://

                        Console.WriteLine("+-----------------------------------------------------------+");
                        Console.WriteLine("|   {0,-5} |   {1,15} |   {2,-10} |   {3,-10} |", "ID", "Server", "RoomCount", "UserCount");
                        Console.WriteLine("+-----------------------------------------------------------+"); ;
                         
                        for (int i = 0; i < agentList.Count; i++)
                        {
                            Console.WriteLine("|   {0,5:###} |   {1,15} |   {2,10:##0} |   {3,10:##0} |", i + 1, servList[i].IP, servList[i].roomCount, servList[i].userCount);
                        }
                        Console.WriteLine("+-----------------------------------------------------------+"); ;
                        Console.WriteLine("##     1. userstate   2. serverstate   3. stopserver       ##"); //commands
                        Console.WriteLine("##     4. startserver   5. restartserver                   ##\n\n");

                        while (true)
                        {
                            string serverControlMSG;

                            Console.Write(">>>");
                            serverControlMSG = Console.ReadLine();

                            if (ServCommandProcess(serverControlMSG))
                            {
                                break;
                            }
                        }
                        break;
                    #endregion

                    #region case AdminState.Monitor:
                    case AdminState.Monitor:
                        while (true) { }
                        Console.WriteLine("+-----------------------------------------------------------+");
                        Console.WriteLine("|   {0,-5} |   {1,15} |   {2,-10} |   {3,-10} |", "ID", "Server", "RoomCount", "UserCount");
                        Console.WriteLine("+-----------------------------------------------------------+"); ;

                        for (int i = 0; i < servList.Count; i++)
                        {
                            if (servList[i].IP == null)
                            {
                                servList[i].sock.Send();//정보줘
                                continue;
                            }
                            Console.WriteLine("|   {0,5:###} |   {1,15} |   {2,10:##0} |   {3,10:##0} |", i + 1, servList[i].IP, servList[i].roomCount, servList[i].userCount);
                        }
                        Console.WriteLine("+-----------------------------------------------------------+"); ;
                        Console.WriteLine("##     1. userstate   2. serverstate                       ##"); //commands
                        Console.WriteLine("##     4. startserver   5. restartserver                   ##\n\n");

                        while (true)
                        {
                            string serverControlMSG;

                            Console.Write(">>>");
                            serverControlMSG = Console.ReadLine();

                            if (ServCommandProcess(serverControlMSG))
                            {
                                break;
                            }
                        }
                        break;
                        #endregion

                }
            }
        }


        //private void Connecting()
        //{
        //    myState = AdminState.None;

        //    //try connect
        //    SocketAsyncEventArgs managerConnect = new SocketAsyncEventArgs();
        //    managerConnect.RemoteEndPoint = myIPEP;

        //    //connect request completed
        //    managerConnect.Completed += new EventHandler<SocketAsyncEventArgs>(Connect_Completed);

        //    //connect request
        //    mySocket.ConnectAsync(managerConnect);
        //}
        private void Connect_Completed(object sender, SocketAsyncEventArgs e)
        {
            mySocket = (Socket)sender;

            
            if (true == mySocket.Connected)
            {
                //Recive Event
                SocketAsyncEventArgs receiveEvent = new SocketAsyncEventArgs();

                receiveEvent.UserToken = mySocket;
                //receive buff
                receiveEvent.SetBuffer(new byte[1024], 0, 1024);
                //receive Event
                receiveEvent.Completed += new EventHandler<SocketAsyncEventArgs>(Recieve_Completed);
                //request receive
                mySocket.ReceiveAsync(receiveEvent);

                myState = AdminState.Connected;

                //timer = new System.Timers.Timer();
                //timer.Interval = 3 * 1000;
                //timer.Elapsed += new System.Timers.ElapsedEventHandler(SendHeartbeat);
                //timer.Start();
            }
            else
            {
                Disconnection();
            }
        }

        private void Recieve_Completed(object sender, SocketAsyncEventArgs e)
        {
            Socket socketClient = (Socket)sender;

            if (true == socketClient.Connected)
            {//연결이 되어 있다.

                //데이터 수신
                //Protocol receiveProtocol = function.bytearraytoprotocol(e.Buffer);

                //명령을 분석 한다.
                //ProtocolAnalysis(receiveProtocol);

                //다음 메시지를 받을 준비를 한다.
                socketClient.ReceiveAsync(e);
            }
            else
            {
                Disconnection();
            }
        }

        //private void ProtocolAnalysis(Protocol protocol)
        //{
        //    switch (protocol.command)
        //    {
        //        //case serverstate
        //        //상태 serverstate로 변환해주고
        //        //바이트 배열을 변환해서 정보 구조체 같은 배열로 변환
        //        //break;
        //        //유저도 같음
        //    }
        //}
        private void SendMSG(ushort command, byte[] data = null)
        {
            //Protocol sendProtocol = new Protocol();
            //SocketAsyncEventArgs sendEvent = new SocketAsyncEventArgs();

            //sendProtocol.command = command;
            //if (data == null)
            //{
            //    sendProtocol.SetData(new byte[0]);
            //}
            //else
            //{
            //    sendProtocol.SetData(data);
            //}

            //byte[] szData = function.ProtocolToByteArray(sendProtocol);
            //sendEvent.SetBuffer(szData, 0, szData.Length);
            //mySocket.SendAsync(sendEvent);
        }
       
        private void Disconnection()
        {
            //mySocket = null;
            //myState = ClientState.Disconnect;
            //if (timer != null)
            //    timer.Stop();
            
        }
 

        /// <summary>
        /// exist command => return true, no exist command => return false;
        /// </summary>
        private bool ServCommandProcess(string input) 
        {
            switch (input.ToUpper())
            {
                case "USERSTATE":
                    //유저인포 요청 센드
                    //myState = AdminState.UserINFO;
                    return true;

                case "SERVERSTATE":
                    //서버 인보 요청 센드
                    //myState = AdminState.ServerState;
                    return true;

                case "STOPSERVER":
                case "STARTSERVER":
                case "RESTARTSERVER":
                    SendServState(input.ToUpper());
                    //새로고침 요청
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// exist command => return true, no exist command => return false;
        /// </summary>
        private bool UserCommandProcess(string input)
        {
            switch (input.ToUpper())
            {
                case "USERSTATE":
                    //유저인포 요청 센드
                    //myState = AdminState.UserINFO;
                    return true;

                case "SERVERSTATE":
                    //서버 인보 요청 센드
                    //myState = AdminState.ServerState;
                    return true;

                case "DELETEUSER":
                    //딜리티 유저 요청
                    return true;

                default:
                    return false;
            }
        }

        private void SendServState(string inputCommand)
        {
            while (true)
            {
                int sNumber;
                Console.Write("ServerNumber : ");
                string servernumberinput = Console.ReadLine();

                if (Int32.TryParse(servernumberinput, out sNumber) && (sNumber > 0 && sNumber < agentList.Count))
                {
                    //해당 번호 요청
                    //myState = (AdminState)Enum.Parse(typeof(AdminState), "Connected");
                    //
                    break;
                }
                else
                {
                    Console.WriteLine("try agin");
                }
            }//while
        }//SendServState*/
    }
}