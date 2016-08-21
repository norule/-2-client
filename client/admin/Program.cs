using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace admin
{
    class Program
    {
        private Socket mySocket;
        private Socket managerSocket;
        private IPEndPoint managerIPEP;
        private AdminState myState;

        private string errormsg="";
        enum AdminState
        {
            None = 0,
            Connected,
            ServerState,
            Rank
        }
        

        static void Main(string[] args)
        {

        }

        public void Process()
        {
            while (true)
            {
                ResetState();
                switch (myState)
                {
                    case AdminState.Connected:
                        int number;

                        Console.WriteLine("-- 1.ServerState -- 2.UserRank --");

                        do
                        {
                            bool numberCheck = Int32.TryParse(Console.ReadLine(), out number);
                            if (!numberCheck)
                            {
                                //에러처리
                                Console.WriteLine("Number Check(Enter)");
                                Console.ReadLine();
                                continue;
                            }

                            if (number == 1) {
                                //serverstate()
                                break;
                            }
                            else if (number == 2)
                            {
                                //userrank()
                                break;
                            }
                            else
                            {
                                Console.WriteLine("Number Check(Enter)");
                                continue;
                            }

                        } while (true);
                        
                        break;
                    case AdminState.None:
                        Console.WriteLine("wait connect");
                        Thread.Sleep(100);
                        break;

                    case AdminState.Rank:
                        Console.WriteLine("+-----------------------------------------------+");
                        Console.WriteLine("|    Rnak      |      ID     |       Count      |");
                        Console.WriteLine("+-----------------------------------------------+");

                        //반복문으로 돌려
                        Console.WriteLine("|    Rnak      |      ID     |       Count      |");
                        //글자 수 포맷 맞추는걸로 맞춰서 하면 됨//https://msdn.microsoft.com/ko-kr/library/system.string.format(v=vs.110).aspx
                        Console.WriteLine("--back--");
                        while (true)
                        {
                            string back;
                            back = Console.ReadLine();
                            if (back.ToUpper() == "BACK")
                            {
                                myState = AdminState.Connected;
                                break;
                            }
                            else
                            {
                                //state non;
                                //요청 새로해
                            }
                        }
                        break;

                    case AdminState.ServerState://
                        Console.WriteLine("+-----------------------------------------------------------+");
                        Console.WriteLine("|   {0,-5} |   {1,15} |   {2,-10} |   {3,-10} |", "ID", "Server", "RoomCount", "UserCount");
                        Console.WriteLine("+-----------------------------------------------------------+"); ;

                        string[] serverlist = new string[0];
                         
                        

                        //반복문으로 돌려
                        for (int i = 0; i < serverlist.Length; i++)
                        {
                            Console.WriteLine("|   {0,5:###} |   {1,15} |   {2,10:###} |   {3,10:###} |", i, test, i * 24, i * 24 + 2);
                        }
                        Console.WriteLine("+-----------------------------------------------------------+"); ;


                        Console.WriteLine("--back--");//명령어목록//userstate, serverstate
                        Console.Write(">>>");
                        while (true)
                        {
                            string serverControlMSG;

                            serverControlMSG = Console.ReadLine();

                            if (serverControlMSG.ToUpper() == "%BACK")
                            {
                                myState = AdminState.Connected;
                                break;
                            }
                            else if (serverControlMSG.ToUpper() == "%REFRESH")
                            {
                                //다시 요청
                                //논으로 바꿨다가
                                //ServerState()
                            }
                            else if (serverControlMSG.ToUpper() == "%OFF")
                            {
                                while (true)
                                {
                                    Console.WriteLine("cencel = %cencel");
                                    Console.Write("ServerNumber :");
                                    serverControlMSG = Console.ReadLine();

                                    if (serverControlMSG.ToUpper().Equals("%CANCEL"))
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        int serverNumber;
                                        bool numberCheck = Int32.TryParse(serverControlMSG, out serverNumber);

                                        if (!numberCheck)
                                        {
                                            Console.WriteLine("Server Number Check(Enter)");
                                            Console.ReadLine();
                                            continue;
                                        }

                                        //입력받은 서버 번호로
                                        //서버 오프 호출
                                        break;
                                    }
                                }
                            }
                            else if (serverControlMSG.ToUpper() == "%ON")
                            {

                            }
                            else if (serverControlMSG.ToUpper() == "%RESTART")
                            {

                            }
                        }
                        break;

                    
                }
            }
        }
        private void ServerState()
        {
            myState = AdminState.None;
            //현재 접속되있는 서버에 대한 정보 요청
        }
        private void UserRank()
        {
            myState = AdminState.None;

        }

        public void Init(String IP, int port)
        {
            managerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            managerIPEP = new IPEndPoint(IPAddress.Parse(IP), port);
        }
        public void Connecting()
        {
            myState = AdminState.None;

            //try connect
            SocketAsyncEventArgs managerConnect = new SocketAsyncEventArgs();
            managerConnect.RemoteEndPoint = managerIPEP;

            //connect request completed
            managerConnect.Completed += new EventHandler<SocketAsyncEventArgs>(Connect_Completed);

            //connect request
            managerSocket.ConnectAsync(managerConnect);
        }
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

        private void ProtocolAnalysis(Protocol protocol)
        {
            switch (protocol.command)
            {
                //case serverstate
                //상태 serverstate로 변환해주고
                //바이트 배열을 변환해서 정보 구조체 같은 배열로 변환
                //break;
                //유저도 같음
            }
        }
        private void SendMSG(ushort command, byte[] data = null)
        {
            Protocol sendProtocol = new Protocol();
            SocketAsyncEventArgs sendEvent = new SocketAsyncEventArgs();

            sendProtocol.command = command;
            if (data == null)
            {
                sendProtocol.SetData(new byte[0]);
            }
            else
            {
                sendProtocol.SetData(data);
            }

            byte[] szData = function.ProtocolToByteArray(sendProtocol);
            sendEvent.SetBuffer(szData, 0, szData.Length);
            mySocket.SendAsync(sendEvent);
        }
        private void ResetState()
        {
            Console.Clear();
            if (errormsg != "")
            {
                Console.WriteLine(errormsg);
            }
        }
        private void Disconnection()
        {
            mySocket = null;
            myState = ClientState.Disconnect;
            if (timer != null)
                timer.Stop();
        }
    }
}