using System;
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

    class Dummy
    {
        //----
        //dummy
        private string IDPW;
        


        private List<ServerControl> serverList;
        private int roomNumber;
        private int Usercommand;
        private List<int> roomList = new List<int>();

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

            Dummy[] dums = new Dummy[20];
            int inx = 1;
            int max = 20;

            for (inx=1; inx <= max; inx++)
            {
                string idpw = "Dummy" + inx.ToString();
                Dummy dummy = new Dummy();
                dummy.Start(idpw);
                dummy.Process();
                dums[inx - 1] = dummy;
                Delay(1000);
            }
            //Dummy dummy = new Dummy();
            //dummy.Start();
            //dummy.Process();
        }
        
        private void Init()
        {
            roomNumber = 0;
            serverList = new List<ServerControl>();
            Usercommand = 0;
            myState = ClientState.Connect;
            errormsg = "";
        }
        public void Start(string _IDPW)
        {
            Init();
            IDPW = _IDPW;
            ServerControl loginServer = new ServerControl("10.100.58.4", 11000);
            loginServer.DataAnalysis = DataAnalysis;
            loginServer.Disconnect = Disconnection;
            loginServer.name = "login";
            loginServer.tryconnect();

            lock (serverList)
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

                        myState = ClientState.Login;
                        break;
                    case ClientState.Login:


                        CFLoginRequest logininfo = new CFLoginRequest(IDPW.ToCharArray(), IDPW.ToCharArray());
                        serverList[0].trysendmsg(HhhHelper.Code.SIGNUP, CFHelper.StructureToByte(logininfo));

                        
                        break;
                    case ClientState.Loading:
                        
                        Console.WriteLine("로딩 중");
                        Delay(100);

                        break;
                    case ClientState.Disconnection:

                        Start(IDPW);
                        break;
                    case ClientState.Lobby:

                        roomNumber = 0;
                        
                        Random rand = new Random();

                        rand.Next(0, roomList.Count);


                        if (roomList.Count > 0)
                        {
                            //random join or create
                            if(rand.Next(0, roomList.Count) % 2 == 0)
                            {
                                roomNumber = rand.Next(0, roomList.Count);

                                CFRoomJoinRequest CFJR = new CFRoomJoinRequest();
                                CFJR.roomNum = roomNumber;

                                if (serverList[0].trysendmsg(HhhHelper.Code.JOIN, CFHelper.StructureToByte(CFJR)))
                                    myState = ClientState.Loading;
                            }
                        }
                        else
                        {
                            //create
                            if (serverList[0].trysendmsg(HhhHelper.Code.CREATE_ROOM))
                                myState = ClientState.Loading;
                        }
                        break;
                    
                    
                    case ClientState.Room:

                        string msg;
                        
                            DateTime currTime = DateTime.Now;
                        int i = 0;
                        do
                        {
                            msg = "";
                            msg = IDPW + currTime.ToString(); ;
                            i++;
                            if(i> (currTime.Millisecond / 100) + 2)
                            {
                                msg = "";
                                msg = "%LEAVE";
                            }
                            MessageProcess(msg);
                            Delay(2000);
                        } while (myState == ClientState.Room);

                        break;
                }
            }
        }
        void CPconnect(ServerControl serv)
        {
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
                case HhhHelper.Code.SIGNUP_FAIL:
                    CFLoginRequest logininfo = new CFLoginRequest(IDPW.ToCharArray(), IDPW.ToCharArray());
                    serverList[0].trysendmsg(HhhHelper.Code.SIGNUP, CFHelper.StructureToByte(logininfo));
                    myState = ClientState.Connect;
                    break;

                case HhhHelper.Code.SIGNUP_SUCCESS:
                    CFLoginRequest sulogininfo = new CFLoginRequest(IDPW.ToCharArray(), IDPW.ToCharArray());
                    serverList[0].trysendmsg(HhhHelper.Code.SIGNUP, CFHelper.StructureToByte(sulogininfo));
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
                    CPControl.name = "room";
                    CPControl.tryconnect();


                    serverList.Add(CPControl);

                    break;

                case HhhHelper.Code.INITIALIZE_FAIL:
                    errormsg = "INITIALIZE_FAIL";
                    serverList?[1].Disconnection();
                    myState = ClientState.Lobby;
                    break;

                    //cp success
                case HhhHelper.Code.INITIALIZE_SUCCESS:
                    errormsg = "INITIALIZE_SUCCESS";
                    lock (serverList)
                    {
                        serverList[0].Disconnection();

                        serverList[0].CPconnect = null;
                    }
                    if(roomNumber==0)
                        serverList[0].trysendmsg(HhhHelper.Code.ROOM_LIST);
                    else
                    {
                        CFRoomJoinRequest CFJR = new CFRoomJoinRequest();
                        CFJR.roomNum = roomNumber;

                        if (serverList[0].trysendmsg(HhhHelper.Code.JOIN, CFHelper.StructureToByte(CFJR)))
                            myState = ClientState.Loading;
                    }
                    break;

                case HhhHelper.Code.ROOM_LIST_FAIL:
                    errormsg = "room list fail";
                    myState = ClientState.Lobby;
                    break;

                case HhhHelper.Code.ROOM_LIST_SUCCESS:
                    errormsg = "ROOM_LIST_SUCCESS";
                    RoomListParsing(protocol.data);
                    myState = ClientState.Lobby;
                    break;

                case HhhHelper.Code.JOIN_SUCCESS:
                    errormsg = "JOIN_SUCCESS";
                    myState = ClientState.Room;
                    break;

                case HhhHelper.Code.JOIN_REDIRECT:
                    try
                    {
                        errormsg = "JOIN_REDIRECT";

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

                    }catch(Exception e)
                    {

                        Console.WriteLine(e.ToString());
                        Console.ReadLine();
                    }
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
                    myState = ClientState.Lobby;
                    break;

                case HhhHelper.Code.CREATE_ROOM_SUCCESS:
                    errormsg = "CREATE_ROOM_SUCCESS";

                    CFRoomCreateResponse roomnum = (CFRoomCreateResponse)CFHelper.ByteToStructure(protocol.data, typeof(CFRoomCreateResponse));
                    roomNumber = roomnum.roomNum;

                    myState = ClientState.Room;
                    break;

                case HhhHelper.Code.LEAVE_ROOM_FAIL:
                    errormsg = "LEAVE_ROOM_FAIL";
                    break;

                case HhhHelper.Code.LEAVE_ROOM_SUCCESS:
                    errormsg = "LEAVE_ROOM_SUCCESS";
                    roomNumber = 0;
                    myState = ClientState.Lobby;
                    break;


                case HhhHelper.Code.MSG_FAIL:
                    errormsg = "MSG_FAIL";
                    break;

                case HhhHelper.Code.MSG:
                    string message = Encoding.UTF8.GetString(protocol.data,0,protocol.data.Length);
                    Console.Write(message+"\n");
                    break;

                case HhhHelper.Code.HEARTBEAT_SUCCESS:
                    Console.WriteLine("HBHBHBHBHBHBHBHBHB");
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
                    myState = ClientState.Loading;
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
            //Console.Clear();
            if (errormsg != "")
            {
                Console.WriteLine(errormsg);
            }
        }

        
        private void RoomListParsing(byte[] input)
        {
            //5;4;8;;8;4; 이런식으로 올꺼임
            roomList = CFHelper.BytesToList(input);
        }

    }
}