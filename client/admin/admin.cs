using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Junhaehok;
using System.Timers;
using System.Threading;

namespace Admin
{
    public delegate void dgDataProcess(AgentControl agent,Packet protocol);
    class Admin
    {
        
        private const int CONNECTIONTIMEOUT = 5;
        private IPEndPoint[] agentsIPEPs;
        private static  AdminState myState;

        private List<AgentControl> agentList = new List<AgentControl>();
        private List<UserHandle> userList = new List<UserHandle>();

        private SocketAsyncEventArgs receiveHeaderEvent = new SocketAsyncEventArgs();
        private SocketAsyncEventArgs sendEvent = new SocketAsyncEventArgs();
         
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



        static void Main(string[] args)
        {
            Admin admin = new Admin();
            admin.Start();
            admin.Process();
        }

        public void Start()
        {
            string[] agentInfo= new string[0];
            try {
                agentInfo = System.IO.File.ReadAllText("agents.conf").Split(',');
            }
            catch (Exception e) {
                Console.WriteLine("\n" + e.Message);
            }
            agentsIPEPs = new IPEndPoint[agentInfo.Count()];
            
            for (int i = 0; i < agentInfo.Count(); i++)
            {
                string[] ipport = agentInfo[i].Split(':');
                agentsIPEPs[i] = new IPEndPoint(IPAddress.Parse(ipport[0]), Int32.Parse(ipport[1]));
            }
            myState = AdminState.None;
            Initagents();
        }

        private void Initagents()
        {
            for (int i = 0; i < agentsIPEPs.Count(); i++)
            {
                AgentControl ag = new AgentControl(agentsIPEPs[i]);
                ag.DataAnalysis = DataAnalysis;
                ag.Connecting();
                agentList.Add(ag);
            }
        }
      

        public void DataAnalysis(AgentControl agent,Packet protocol)
        {
            switch (protocol.header.code)
            {
                case HhhHelper.Code.SERVER_RESTART_SUCCESS:
                case HhhHelper.Code.SERVER_START_SUCCESS:
                    break;

                case HhhHelper.Code.SERVER_STOP_SUCCESS:
                    break;
                 
                case HhhHelper.Code.SERVER_INFO_SUCCESS:
                    if (myState == AdminState.None)
                    {
                        myState = AdminState.Monitor;
                    }
                    AAServerInfoResponse serverinfo = (AAServerInfoResponse)AAHelper.ByteToStructure(protocol.data, typeof(AAServerInfoResponse));
                    agent.roomCount = serverinfo.roomCount;
                    agent.userCount = serverinfo.userCount;
                    agent.alive = serverinfo.alive;
                    break;

                case HhhHelper.Code.RANKINGS_SUCCESS:
                    //화면 새로 고침
                    myState = AdminState.UserINFO;
                    break;


                    //페일류 에러메시짘
                case HhhHelper.Code.SERVER_START_FAIL:
                    break;

                case HhhHelper.Code.SERVER_RESTART_FAIL:
                    break;

                case HhhHelper.Code.SERVER_STOP_FAIL:
                    break;

                case HhhHelper.Code.SERVER_INFO_FAIL:
                    break;

                case HhhHelper.Code.RANKINGS_FAIL:
                    break;

                default:
                    break;
            }
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
                        Console.WriteLine("wait");
                        Delay(100);
                        break;
                    #endregion

                    #region case AdminState.Rank:
                    case AdminState.UserINFO:

                        Console.WriteLine("+----------------------------------------+");
                        Console.WriteLine("|  {0,5} |   {1,12} |   {2,10} |", "Rnak", "ID", "Count");
                        Console.WriteLine("+----------------------------------------+");

                        for (int i = 0; i < userList.Count; i++)
                        {
                            Console.WriteLine("|   {0,4} |   {1,12} |   {2,10} |", i + 1, userList[i].ID, userList[i].MSGCOUNT);
                        }

                        Console.WriteLine("+----------------------------------------+");

                        Console.WriteLine("##   1. userstate     2. serverstate    ##");
                        Console.WriteLine("##   3. monitor       4. deleteUser     ##\n\n");


                        while (true)
                        {
                            string userControlMSG;

                            Console.Write(">>>");
                            userControlMSG = Console.ReadLine();
                             
                            UserCommandProcess(userControlMSG);
                        }
                        break;
                    #endregion

                    #region case AdminState.ServerState:
                    case AdminState.ServerState://

                        Console.WriteLine("+--------------------------------------------------------------------+");
                        Console.WriteLine("|   {0,-5} |   {1,15} |   {2,-10} |   {3,-10} |  {4,-5} |", "ID", "Server", "RoomCount", "UserCount", "alive");
                        Console.WriteLine("+--------------------------------------------------------------------+");

                        for (int i = 0; i < agentList.Count; i++)
                        {
                            Console.WriteLine("|   {0,5:###} |   {1,15} |   {2,10:##0} |   {3,10:##0} |  {4,5} |", i + 1, agentList[i].myIPEP.Port, agentList[i].roomCount, agentList[i].userCount, agentList[i].alive);
                        }
                        Console.WriteLine("+--------------------------------------------------------------------+");
                        Console.WriteLine("##     1. userstate      2. serverstate        3. SERVER_STOP       ##");
                        Console.WriteLine("##     4. SERVER_START   5. SERVER_RESTART   6. MONITOR             ##\n\n");

                        while (true)
                        {
                            Console.Write(">>>");
                            string inputnum = Console.ReadLine();
                            int command;
                            if (Int32.TryParse(inputnum, out command))
                            {
                                if (ServCommandProcess(command))
                                {
                                    break;
                                }
                            }
                        }
                        break;
                    #endregion

                    #region case AdminState.Monitor:
                    case AdminState.Monitor:

                        Console.WriteLine("+--------------------------------------------------------------------+");
                        Console.WriteLine("|   {0,-5} |   {1,15} |   {2,-10} |   {3,-10} |  {4,-5} |", "ID", "Server", "RoomCount", "UserCount", "alive");
                        Console.WriteLine("+--------------------------------------------------------------------+");

                        for (int i = 0; i < agentList.Count; i++)
                        {
                            Console.WriteLine("|   {0,5:###} |   {1,15} |   {2,10:##0} |   {3,10:##0} |  {4,5} |", i + 1, agentList[i].myIPEP.Port, agentList[i].roomCount, agentList[i].userCount, agentList[i].alive);
                        }

                        Console.WriteLine("+--------------------------------------------------------------------+");
                        Console.WriteLine("##             1. userstate              2. serverstate             ##\n"); //commands

                        string input = Console.ReadLine();
                        int Usercommand;
                        if (Int32.TryParse(input, out Usercommand))
                        {
                            if (Usercommand == 1)
                            {
                                myState = AdminState.UserINFO;
                                break;
                            }
                            else if (Usercommand == 2)
                            {
                                myState = AdminState.ServerState;
                                break;
                            }
                        }

                        break;
                        #endregion

                }
            }
        }

        /// <summary>
        /// exist command => return true, no exist command => return false;
        /// </summary>
        private bool ServCommandProcess(int input) 
        {
            switch (input)
            {
                case 1:
                    //유저인포 요청 센드
                    myState = AdminState.UserINFO;
                    return true;

                case 2:
                    ServerinfoRequestandNone();
                    return true;

                case 6:
                    ServerinfoRequestandNone();
                    return true;

                case 3:
                case 4:
                case 5:
                    while (true)
                    {
                        int sNumber;
                        Console.Write("ServerNumber : ");
                        string servernumberinput = Console.ReadLine();

                        if (Int32.TryParse(servernumberinput, out sNumber))
                        {
                            if ((sNumber > 0 && sNumber <= agentList.Count))
                            {
                                if (input == 3)
                                    agentList[sNumber-1].SendMSG(HhhHelper.Code.SERVER_STOP);
                                else if (input == 4)
                                    agentList[sNumber-1].SendMSG(HhhHelper.Code.SERVER_START);
                                else if (input == 5)
                                    agentList[sNumber-1].SendMSG(HhhHelper.Code.SERVER_RESTART);
                                break;
                            }
                            else
                            {
                                Console.WriteLine("try agin");
                            }
                        }
                        

                    }//while
                    Thread.Sleep(100);
                    ServerinfoRequestandNone();
                    return true;

                default:
                    return false;
            }
        }
        private void LiveAgent(ushort input, byte[] data=null )
        {
            for (int i = 0; i < agentList.Count; i++)
            {
                if (agentList[i].adminSock.Connected)
                {
                    agentList[i].SendMSG(input, data);
                    break;
                }
            }
        }

        private void ServerinfoRequestandNone()
        {
            for (int i = 0; i < agentList.Count; i++)
            {
                agentList[i].SendMSG(HhhHelper.Code.SERVER_INFO);
            }
            myState = AdminState.None;
        }
        /// <summary>
        /// exist command => return true, no exist command => return false;
        /// </summary>
        private bool UserCommandProcess(string input)
        {// 1. userstate  2. serverstate 3. monitor       4. deleteUser 
            switch (input.ToUpper())
            {
                case "1":
                    LiveAgent(HhhHelper.Code.RANKINGS);
                    return true;

                case "2":
                    LiveAgent(HhhHelper.Code.SERVER_INFO);
                    myState = AdminState.ServerState;
                    return true;

                case "3":
                    LiveAgent(HhhHelper.Code.SERVER_INFO);
                    myState = AdminState.Monitor;
                    return true;

                case "4":

                    Console.Write("USER ID :");
                    string deleteID = Console.ReadLine();

                    AADeleteUserRequest ADR = new AADeleteUserRequest(deleteID);
                    LiveAgent(HhhHelper.Code.DELETE_USER,AAHelper.StructureToByte(ADR));

                    return true;

                default:
                    return false;
            }
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
    }
}