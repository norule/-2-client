using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Junhaehok;
using CF_Protocol;

namespace Dummy
{
    public delegate void dgConnect(ServerControl server);
    public delegate void dgDisconnect(ServerControl serv);
    public delegate void dgDataProcess(ServerControl serv, Packet protocol);

    class Dummy
    {
        private string IDPW;
        
        private List<ServerControl> serverList;
        private List<int> roomList = new List<int>();
        private CFSigninResponse roomConnectionINFO = default(CFSigninResponse);
        private int roomNumber;

        private DummyState myState = DummyState.Loading;

        private const int MAXIMUM_ID_LENGTH = 12;
        private const int MAXIMUM_PW_LENGTH = 18;

        private int initializeCount = 5;
        
        
        enum DummyState
        {
            None=0,
            Connect,
            Login,
            Loading,
            Lobby,
            Room,
            Disconnection
        }
        
        private void Init()
        {
            roomNumber = 0;
            serverList = new List<ServerControl>();
            myState = DummyState.None;
        }
        public void Start(string _IDPW)
        {
            Init();
            IDPW = _IDPW;
            ServerControl loginServer = new ServerControl("10.100.58.4", 10000); //login servr
            loginServer.DataAnalysis = DataAnalysis;
            loginServer.Disconnect = Disconnection;
            loginServer.name = "login";
            loginServer.tryconnect();

            lock (serverList)
                serverList.Add(loginServer);

            Process();
        }

        public void Process()
        {
            while (true)
            {
                switch (myState)
                {
                    case DummyState.None:
                        Console.WriteLine(myState);
                        Delay(100);
                        break;
                    case DummyState.Connect:
                        Console.WriteLine(myState);
                        myState = DummyState.Login;
                        break;
                    case DummyState.Login:
                        //try signin
                        Console.WriteLine(myState);
                        CFLoginRequest logininfo = new CFLoginRequest(IDPW.ToCharArray(), IDPW.ToCharArray());
                        serverList[0].trysendmsg(HhhHelper.Code.SIGNIN, CFHelper.StructureToByte(logininfo));
                        myState = DummyState.Loading;
                        break;
                    case DummyState.Loading:
                        Console.WriteLine(myState);
                        Delay(100);
                        break;
                    case DummyState.Disconnection:
                        Console.WriteLine(myState);
                        //Start(IDPW);
                        break;
                    case DummyState.Lobby:
                        roomNumber = 0;
                        Console.WriteLine(myState);

                        //random join or create
                        if (roomList.Count > 0)
                        {
                            Random rand = new Random();
                            rand.Next(roomList.Count);

                            if (rand.Next(100) % 3 == 0)
                            {
                                if (serverList[0].trysendmsg(HhhHelper.Code.CREATE_ROOM))
                                    myState = DummyState.Loading;
                            }
                            else
                            {
                                roomNumber = rand.Next(roomList.Count);

                                CFRoomJoinRequest CFJR = new CFRoomJoinRequest();
                                CFJR.roomNum = roomNumber;

                                if (serverList[0].trysendmsg(HhhHelper.Code.JOIN, CFHelper.StructureToByte(CFJR)))
                                    myState = DummyState.Loading;
                            }
                        }
                        else
                        {
                            //create
                            if (serverList[0].trysendmsg(HhhHelper.Code.CREATE_ROOM))
                                myState = DummyState.Loading;
                        }

                        break;
                    case DummyState.Room:
                        string msg;
                        Console.WriteLine(myState);
                        DateTime currTime = DateTime.Now;
                        int i = 0;
                        do
                        {
                            msg = "";
                            msg = IDPW +" : "+ currTime.ToString(); ;
                            
                            if (i++ > (currTime.Millisecond / 100) + 2)
                            {
                                msg = "";
                                msg = "%LEAVE";
                            }
                            MessageProcess(msg);
                            Delay(2000);
                        } while (myState == DummyState.Room);

                        break;
                }
            }
        }
        void CPconnect(ServerControl serv)
        {
            //try connection passing
            CFInitializeRequest CFCR = new CFInitializeRequest();
            CFCR.cookie = roomConnectionINFO.cookie;
            serv.trysendmsg(HhhHelper.Code.INITIALIZE, CFHelper.StructureToByte(CFCR));
        }


        public static long uid = 0;
        //data analysis
        public  void DataAnalysis(ServerControl serv,Packet protocol)
        {
            if(uid==0)
                uid = protocol.header.uid;

            switch (protocol.header.code)
            {
                case HhhHelper.Code.DUMMY_SIGNUP_FAIL:
                    Console.WriteLine("SIGNUP_FAIL");
                    Environment.Exit(0);
                    break;

                case HhhHelper.Code.DUMMY_SIGNUP_SUCCESS:
                    Console.WriteLine("SIGNUP_SUCCESS");
                    CFLoginRequest sulogininfo = new CFLoginRequest(IDPW.ToCharArray(), IDPW.ToCharArray());
                    serverList[0].trysendmsg(HhhHelper.Code.SIGNIN, CFHelper.StructureToByte(sulogininfo));
                    break;

                case HhhHelper.Code.SIGNIN_FAIL:
                    Console.WriteLine("SIGNIN_FAIL");
                    sulogininfo = new CFLoginRequest(IDPW.ToCharArray(), IDPW.ToCharArray());
                    serverList[0].trysendmsg(HhhHelper.Code.DUMMY_SIGNUP, CFHelper.StructureToByte(sulogininfo));
                    break;

                case HhhHelper.Code.SIGNIN_SUCCESS:
                    //cookie
                    Console.WriteLine("SIGNIN_SUCCESS");
                    roomConnectionINFO = (CFSigninResponse)CFHelper.ByteToStructure(protocol.data, typeof(CFSigninResponse));
                    
                    //try connection passing
                    string IP = new string(roomConnectionINFO.ip).Replace("\0", string.Empty);
                    ServerControl CPControl = new ServerControl(IP, roomConnectionINFO.port);
                    CPControl.Disconnect = Disconnection;
                    CPControl.DataAnalysis = DataAnalysis;
                    CPControl.CPconnect = CPconnect;
                    CPControl.name = "room";
                    CPControl.tryconnect();

                    serverList.Add(CPControl);
                    break;

                case HhhHelper.Code.INITIALIZE_FAIL:
                    //try reInitialize request
                    if (initializeCount > 0)
                    {
                        serverList[1].tryconnect();
                        break;
                    }
                    serverList[1].Disconnection();
                    break;

                    
                case HhhHelper.Code.INITIALIZE_SUCCESS:
                    initializeCount = 5;

                    lock (serverList)
                    {
                        serverList[0].Disconnection();
                        serverList[0].CPconnect = null;
                    }
                    //room list request
                    if(roomNumber==0)
                        serverList[0].trysendmsg(HhhHelper.Code.ROOM_LIST);
                    else
                    {
                        CFRoomJoinRequest CFJR = new CFRoomJoinRequest();
                        CFJR.roomNum = roomNumber;
                        if (serverList[0].trysendmsg(HhhHelper.Code.JOIN, CFHelper.StructureToByte(CFJR)))
                            myState = DummyState.Loading;
                    }
                    
                    break;

                case HhhHelper.Code.ROOM_LIST_FAIL:
                    myState = DummyState.Lobby;
                    break;

                case HhhHelper.Code.ROOM_LIST_SUCCESS:
                    RoomListParsing(protocol.data);
                    myState = DummyState.Lobby;
                    break;

                case HhhHelper.Code.JOIN_SUCCESS:
                    myState = DummyState.Room;
                    break;

                case HhhHelper.Code.JOIN_REDIRECT:
                        //other server
                        CFRoomJoinRedirectResponse JRD = new CFRoomJoinRedirectResponse();
                        JRD = (CFRoomJoinRedirectResponse)CFHelper.ByteToStructure(protocol.data, typeof(CFRoomJoinRedirectResponse));

                        string JRD_IP = new string(JRD.ip).Replace("\0", string.Empty);
                        roomConnectionINFO.cookie = JRD.cookie;

                        ServerControl JoinCP = new ServerControl(JRD_IP, JRD.port);
                        JoinCP.Disconnect = Disconnection;
                        JoinCP.DataAnalysis = DataAnalysis;
                        JoinCP.CPconnect = CPconnect;
                        JoinCP.tryconnect();

                        serverList.Add(JoinCP);
                    break;
             
                case HhhHelper.Code.JOIN_FULL_FAIL:
                    myState = DummyState.Lobby;
                    break;

                case HhhHelper.Code.JOIN_NULL_FAIL:
                    myState = DummyState.Lobby;
                    break;

                case HhhHelper.Code.CREATE_ROOM_FAIL:
                    myState = DummyState.Lobby;
                    break;

                case HhhHelper.Code.CREATE_ROOM_SUCCESS:
                    CFRoomCreateResponse roomnum = (CFRoomCreateResponse)CFHelper.ByteToStructure(protocol.data, typeof(CFRoomCreateResponse));
                    roomNumber = roomnum.roomNum;

                    myState = DummyState.Room;
                    break;

                case HhhHelper.Code.LEAVE_ROOM_SUCCESS:
                    roomNumber = 0;
                    myState = DummyState.Lobby;
                    break;
                     
                case HhhHelper.Code.MSG:
                    string message = Encoding.UTF8.GetString(protocol.data,0,protocol.data.Length);
                    break;

                case HhhHelper.Code.HEARTBEAT_SUCCESS:
                    myState = DummyState.Connect;
                    break;

                case HhhHelper.Code.HEARTBEAT:
                    serv.trysendmsg(HhhHelper.Code.HEARTBEAT_SUCCESS);
                    break;
            }
        }
        
        private void MessageProcess(string inpuMSG)
        {
            //check command
            switch (inpuMSG.ToUpper())
            {
                case "%LEAVE":
                    myState = DummyState.Loading;
                    serverList[0].trysendmsg(HhhHelper.Code.LEAVE_ROOM);
                    break;

                case "%LOGOUT":
                    serverList[0].trysendmsg(HhhHelper.Code.SIGNOUT);
                    Start(IDPW);
                    break;

                default:
                    serverList[0].trysendmsg(HhhHelper.Code.MSG, Encoding.UTF8.GetBytes(inpuMSG));
                    break;
            }
        }
        private void Disconnection(ServerControl serv )
        {
            lock (serverList)
                serverList.Remove(serv);

            serv = default(ServerControl);

            if(serverList.Count==0)
                myState = DummyState.Disconnection;
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

        private void RoomListParsing(byte[] input)
        {
            roomList = CFHelper.BytesToList(input);
        }

    }
}