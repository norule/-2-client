﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Junhaehok;
using CF_Protocol;

namespace client
{
    public delegate void dgConnect(ServerControl server);
    public delegate void dgDisconnect(ServerControl serv);
    public delegate void dgDataProcess(ServerControl serv, Packet protocol);

    class Client
    {
        private List<ServerControl> serverList = new List<ServerControl>();
        
        private int roomNumber;
        private int Usercommand;
        private string[] roomList = new string[0];

        private ClientState myState = ClientState.Loading;
        private CFSigninResponse roomConnectionINFO = default(CFSigninResponse);

        private string errormsg = "";

        private const int MAXIMUM_ID_LENGTH = 12;
        private const int MAXIMUM_PW_LENGTH = 18;
        
        //client state
        enum ClientState
        {
            None=0,
            Connect,
            Login,
            Loading,
            Lobby,
            Room,
            Disconnection
        }


        static void Main(string[] args)
        {
            Client client = new Client();
            client.Start();
            client.Process();
        }
        
        public void Start()
        {
            //myState = ClientState.None;
            myState = ClientState.Connect;
            //Usercommand = 1;


            ServerControl loginServer = new ServerControl("10.100.58.4", 11000);
            //ServerControl loginServer = new ServerControl("127.0.0.1", 11000);
            loginServer.DataAnalysis = DataAnalysis;
            loginServer.Disconnect = Disconnection;
            loginServer.Connecting();
            serverList.Add(loginServer);
        }

        public void Process()
        {
            while (true)
            {
                ResetState();                                       
                switch (myState)
                {
                    case ClientState.None:
                        Console.WriteLine("Waiting");
                        Delay(100);
                        break;
                    case ClientState.Connect:

                        Console.WriteLine("1. Signup  2. Signin 3. Exit");

                        while (true)
                        {
                            Console.Write(">>>");
                            string input = Console.ReadLine();
                            

                            if(Int32.TryParse(input,out Usercommand))
                            {
                                if(Usercommand>=1 && Usercommand <= 2)
                                {
                                    break;
                                }
                                if (Usercommand == 3)
                                    return;
                            }
                        }
                        myState = ClientState.Login;
                        break;


                    //connected , Login
                    #region case ClientState.Login:
                    case ClientState.Login:

                        string ID = null;
                        string PW = null;

                        //write ID
                        Console.Write("ID : ");
                        ID = Console.ReadLine();

                        if (0 >= ID.Length || MAXIMUM_ID_LENGTH <= ID.Length)
                        {
                            Console.WriteLine("ID length check(Enter)");
                            Console.ReadLine();
                            break;
                        }

                        //write Password
                        Console.Write("Password : ");
                        PW = Console.ReadLine();

                        if (0 >= PW.Length || MAXIMUM_PW_LENGTH <= PW.Length)
                        {
                            Console.WriteLine("Password length check(Enter)");
                            Console.ReadLine();
                            break;
                        }

                        CFLoginRequest logininfo = new CFLoginRequest(ID.ToCharArray(), PW.ToCharArray());
                        
                        //request login
                        switch (Usercommand) 
                        {
                            case 1:
                                if (serverList[0].SendMSG(HhhHelper.Code.SIGNUP, CFHelper.StructureToByte(logininfo)))
                                    myState = ClientState.Loading;
                                break;

                            case 2:
                                if (serverList[0].SendMSG(HhhHelper.Code.SIGNIN, CFHelper.StructureToByte(logininfo)))
                                    myState = ClientState.Loading;
                                break;
                        }

                        break;
                    #endregion case ClientState.Login:
                    //loading
                    #region case ClientState.loading :
                    case ClientState.Loading:
                        
                        Console.WriteLine("로딩 중");
                        Delay(100);

                        break;
                    #endregion loading
                    //Try reconnection
                    #region case ClientState.Disconnect:
                    case ClientState.Disconnection:

                        Console.WriteLine("*** Disconnected ***");
                        Console.WriteLine("try agin? (y/n)");
                        char d = Console.ReadKey().KeyChar;

                        do
                        {
                            if (d == 'y' || d == 'Y')
                            {
                                //try conntect loginserver
                                Start();
                                break;
                            }
                            else if (d == 'n' || d == 'N')
                            {
                                return;
                            }
                            else
                            {
                                Console.WriteLine("\nY or N only");
                                d = Console.ReadKey().KeyChar;
                            }
                        } while (true);

                        Console.WriteLine();
                        break;
                    #endregion case ClientState.Disconnect:
                    //in lobby , createRoom() , JoinRoom()
                    #region case ClientState.Lobby :
                    case ClientState.Lobby:

                        roomNumber = 0;
                        string lobbyMsg;

                        Console.WriteLine("-- %Create / %Join / %ModifyPassword / %DELETEACCOUNT --");
                        Console.WriteLine("--                      Room List                     --");
                        Console.WriteLine("--------------------------------------------------------");
                        for (int i = 0; i < roomList.Count(); i++)
                        {
                            Console.Write("Room : " + string.Format("{0:000}", roomList[i])+"  ");
                            if (i % 3 == 0)
                                Console.WriteLine();
                        }

                        while (true)
                        {
                            Console.Write(">>> :");
                            lobbyMsg = Console.ReadLine();

                            if (lobbyMsg.ToUpper().Equals("%JOIN"))
                            {
                                Console.WriteLine("Room Number :");
                                bool numberCheck = Int32.TryParse(Console.ReadLine(), out roomNumber);

                                if (!numberCheck)
                                {
                                    Console.WriteLine("Romm Number Check(Enter)");
                                    Console.ReadLine();
                                    continue;
                                }
                                CFRoomJoinRequest CFJR = new CFRoomJoinRequest();
                                CFJR.roomNum = roomNumber;

                                if (serverList[0].SendMSG(HhhHelper.Code.JOIN, CFHelper.StructureToByte(CFJR)))
                                    myState = ClientState.Loading;
                                break;
                            }
                            else if (lobbyMsg.ToUpper().Equals("%CREATE"))
                            {
                                if (serverList[0].SendMSG(HhhHelper.Code.CREATE_ROOM))
                                    myState = ClientState.Loading;
                                break;
                            }
                            else if (lobbyMsg.ToUpper().Equals("%ModifyPassword"))
                            {
                                PW = null;
                                while (true)
                                {
                                    Console.Write("Password : ");
                                    PW = Console.ReadLine();

                                    if (0 >= PW.Length || MAXIMUM_PW_LENGTH <= PW.Length)
                                    {
                                        Console.WriteLine("Password length check(Enter)");
                                        Console.ReadLine();
                                        break;
                                    }
                                }
                                CFUpdateUserRequest CFUR = new CFUpdateUserRequest(PW.ToCharArray());
                                serverList[0].SendMSG(HhhHelper.Code.UPDATE_USER, CFHelper.StructureToByte(CFUR));

                                break;
                            }
                            else if (lobbyMsg.ToUpper().Equals("%DELETEACCOUNT"))
                            {
                                if (serverList[0].SendMSG(HhhHelper.Code.DELETE_USER))
                                    myState = ClientState.Loading;
                                break;
                            }
                            else
                            {
                                Console.WriteLine("check commands");
                            }
                        }
                        break;
                    #endregion
                    //in Room , LeaveRoom()
                    #region case ClientState.Room:
                    case ClientState.Room:

                        string msg;
                        
                        Console.WriteLine("Enter Room : " + string.Format("{0:000}", roomNumber));
                        Console.WriteLine("-- %Leave / %Logout--");
                        do
                        {
                            Console.Write("send :");
                            msg = Console.ReadLine();

                            MessageProcess(msg);
                        } while (myState == ClientState.Room);

                        break;

                        #endregion
                }
            }
        }
        void CPconnect(ServerControl serv)
        {
            CFInitializeRequest CFCR = new CFInitializeRequest();
            CFCR.cookie = roomConnectionINFO.cookie;
            serv.SendMSG(HhhHelper.Code.INITIALIZE, CFHelper.StructureToByte(CFCR));
        }


        public static long uid = 0;
        //data analysis
        public  void DataAnalysis(ServerControl serv,Packet protocol)
        {
            //스테이트 변경하고 Usercommand지우면 될꺼야 아마
            //나중에 하트비트에 옮겨
            if(uid!=0)
                uid = protocol.header.uid;

            switch (protocol.header.code)
            {
                case HhhHelper.Code.SIGNUP_FAIL:
                    errormsg = "SIGNUP_FAIL";
                    myState = ClientState.Connect;
                    break;

                case HhhHelper.Code.SIGNUP_SUCCESS:
                    errormsg = "SIGNUP_SUCCESS";
                    myState = ClientState.Connect;
                    break;

                case HhhHelper.Code.UPDATE_USER_USER_FAIL:
                    errormsg = "UPDATE_USER_FAIL";
                    break;

                case HhhHelper.Code.UPDATE_USER_SUCCESS:
                    errormsg = "UPDATE_USER_SUCCESS";
                    break;

                case HhhHelper.Code.DELETE_USER_FAIL:
                    errormsg = "DELETE_USER_FAIL";
                    break;

                case HhhHelper.Code.DELETE_USER_SUCCESS:
                    errormsg = "DELETE_USER_SUCCESS";
                    myState = ClientState.Login;
                    break;

                case HhhHelper.Code.SIGNIN_FAIL:
                    errormsg = "SIGNIN_FAIL";
                    myState = ClientState.Connect;
                    break;

                case HhhHelper.Code.SIGNIN_SUCCESS:
                    errormsg = "SIGNIN_SUCCESS";

                    //cookie
                    roomConnectionINFO = (CFSigninResponse)CFHelper.ByteToStructure(protocol.data, typeof(CFSigninResponse));
                    Console.WriteLine(roomConnectionINFO);
                    string IP = new string(roomConnectionINFO.ip).Replace("\0", string.Empty);
                    ServerControl CPControl = new ServerControl(IP, roomConnectionINFO.port);
                    CPControl.Disconnect = Disconnection;
                    CPControl.DataAnalysis = DataAnalysis;
                    CPControl.CPconnect = CPconnect;
                    CPControl.Connecting();
                    
                    serverList.Add(CPControl);

                    break;

                case HhhHelper.Code.INITIALIZE_FAIL:
                    errormsg = "INITIALIZE_FAIL";
                    serverList[1].Disconnection();
                    break;

                    //cp success
                case HhhHelper.Code.INITIALIZE_SUCCESS:
                    serverList[0].Disconnection();
                    serverList[0].CPconnect = null;

                    if(roomNumber==0)
                        serverList[0].SendMSG(HhhHelper.Code.ROOM_LIST);
                    else
                    {
                        CFRoomJoinRequest CFJR = new CFRoomJoinRequest();
                        CFJR.roomNum = roomNumber;

                        if (serverList[0].SendMSG(HhhHelper.Code.JOIN, CFHelper.StructureToByte(CFJR)))
                            myState = ClientState.Loading;
                    }
                    break;

                case HhhHelper.Code.ROOM_LIST_FAIL:
                    errormsg = "room list fail";
                    myState = ClientState.Lobby;
                    break;

                case HhhHelper.Code.ROOM_LIST_SUCCESS:
                    RoomListParsing(protocol.data);
                    myState = ClientState.Lobby;
                    break;

                case HhhHelper.Code.JOIN_SUCCESS:

                    myState = ClientState.Room;
                    break;

                case HhhHelper.Code.JOIN_REDIRECT:

                    ServerControl JoinCP = new ServerControl(new string(roomConnectionINFO.ip), roomConnectionINFO.port);

                    JoinCP.Disconnect = Disconnection;
                    JoinCP.DataAnalysis = DataAnalysis;
                    JoinCP.CPconnect = CPconnect;
                    JoinCP.Connecting();

                    serverList.Add(JoinCP);
                    
                    break;

                case HhhHelper.Code.JOIN_FAIL:
                    errormsg = "join room fail";
                    break;

                case HhhHelper.Code.JOIN_FULL_FAIL:
                    errormsg = "Room Full";
                    myState = ClientState.Lobby;
                    break;

                case HhhHelper.Code.JOIN_NULL_FAIL:
                    errormsg = "Room not Exist";
                    myState = ClientState.Lobby;
                    break;

                case HhhHelper.Code.CREATE_ROOM_FAIL:
                    errormsg = "Create Room Fail";
                    //실패 이유를 바디로 주려나?
                    myState = ClientState.Lobby;
                    break;

                case HhhHelper.Code.CREATE_ROOM_SUCCESS:
                    myState = ClientState.Room;
                    break;

                case HhhHelper.Code.LEAVE_ROOM_FAIL:
                    errormsg = "LEAVE_ROOM_FAIL";
                    break;

                case HhhHelper.Code.LEAVE_ROOM_SUCCESS:
                    roomNumber = 0;
                    myState = ClientState.Lobby;
                    break;

                case HhhHelper.Code.SIGNOUT_FAIL:
                    errormsg = "SIGNOUT_FAIL";
                    break;

                case HhhHelper.Code.SIGNOUT_SUCCESS:
                    roomNumber = 0;
                    myState = ClientState.Login;
                    break;

                case HhhHelper.Code.MSG_FAIL:
                    errormsg = "MSG_FAIL";
                    break;

                case HhhHelper.Code.MSG_SUCCESS:
                    string message = Encoding.UTF8.GetString(protocol.data,0,protocol.data.Length);
                    Console.Write(message);
                    break;

                case HhhHelper.Code.HEARTBEAT_SUCCESS:
                    if (myState == ClientState.None)
                    {
                        myState = ClientState.Connect;
                    }
                    break;

                case HhhHelper.Code.HEARTBEAT:
                    serv.SendMSG(HhhHelper.Code.HEARTBEAT_SUCCESS);
                    break;
            }
        }
        
        private void MessageProcess(string inpuMSG)
        {
            //check command
            switch (inpuMSG.ToUpper())
            {
                case "%LEAVE":
                    myState = ClientState.Loading;
                    serverList[0].SendMSG(HhhHelper.Code.LEAVE_ROOM);
                    break;

                case "%LOGOUT":
                    myState = ClientState.Loading;
                    serverList[0].SendMSG(HhhHelper.Code.SIGNOUT);
                    break;

                default:
                    serverList[0].SendMSG(HhhHelper.Code.MSG, Encoding.UTF8.GetBytes(inpuMSG));
                    break;
            }
        }
        private void Disconnection(ServerControl serv )
        {

            serverList.Remove(serv);
            serv = default(ServerControl);

            if(serverList.Count==0)
                myState = ClientState.Disconnection;
        }

        private static DateTime Delay(int MS)
        {
            DateTime ThisMoment = DateTime.Now;
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, MS);
            DateTime AfterWards = ThisMoment.Add(duration);

            while (AfterWards >= ThisMoment)
            {
                ThisMoment = DateTime.Now;
            }

            return DateTime.Now;
        }  

        private void ResetState()
        {
            Console.Clear();
            if (errormsg != "")
            {
                Console.WriteLine(errormsg);
            }
        }

        
        private void RoomListParsing(byte[] input)
        {
            //5;4;8;;8;4; 이런식으로 올꺼임
            roomList = Encoding.UTF8.GetString(input).Split(';');
        }

    }
}