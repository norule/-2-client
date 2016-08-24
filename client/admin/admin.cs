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
using static Lognamespace.cLog;         
namespace Admin
{
    public delegate void dgDisconnection(AgentControl agn);
    public delegate void dgOnconnection(AgentControl agn);
    public delegate void dgDataProcess(AgentControl agent, Packet protocol);
    class Admin
    {
        private const int CONNECTIONTIMEOUT = 5;

        private static AdminState myState;
        private List<agentinfo> agentinfoList = new List<agentinfo>();

        private List<AgentControl> agentList = new List<AgentControl>();
        private List<UserHandle> userList = new List<UserHandle>();
        private System.Timers.Timer reConnectrequest = new System.Timers.Timer();

        private StringBuilder userinput = new StringBuilder();                          //command
        private CommnadTYPE command = 0;                                                //use to request command
        private int serverNUM = 0;

        enum AdminState
        {
            SERVERSTATE,
            USERSTATE
        }

        struct agentinfo
        {
            public string type;
            public IPEndPoint ipep;

            public agentinfo(string _tyep, IPEndPoint _ipep)
            {
                type = _tyep;
                ipep = _ipep;
            }
        }                         //agent IPEP
        enum CommnadTYPE                            //sendmessage command code
        {
            None = 0,
            SERVER_START = 1200,
            SERVER_STOP = 1270,
            SERVER_RESTART = 1240,
        }

        static void Main(string[] args)
        {
            Admin admin = new Admin();
            admin.Start();
        }

        public void Start()
        {
            Log("start");
            userinput.Append(">> : ");
            string[] agentInfo = new string[0];
            //agents ip read
            try
            {
                agentInfo = System.IO.File.ReadAllText("agents.conf").Split(',');
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.Message);
            }

            //agent info
            for (int i = 0; i < agentInfo.Count(); i++)
            {
                string[] agentinfo = agentInfo[i].Split(':');
                agentinfoList.Add(new agentinfo(agentinfo[0], new IPEndPoint(IPAddress.Parse(agentinfo[1]), Int32.Parse(agentinfo[2]))));
            }

            reConnectrequest.Interval = 2000;
            reConnectrequest.Elapsed += Initagents;
            reConnectrequest.Start();
            myState = AdminState.SERVERSTATE;

            InputProcess();
            PrintState();
            Initagents();
        }

        private void Initagents(object sender = null, ElapsedEventArgs e = null)
        {
            for (int i = 0; i < agentinfoList.Count(); )
            {//init connect agent
                AgentControl ag = new AgentControl(agentinfoList[i].type, agentinfoList[i].ipep);
                ag.DataAnalysis = DataAnalysis;
                ag.removeThisConnectioinlist = DisConnection;
                ag.removeThisWaitList = OnConnection;
                ag.Connecting();
                agentinfoList.Remove(new agentinfo(ag.type, ag.myIPEP));
            }
        }

        public void DataAnalysis(AgentControl agent, Packet protocol)
        {
            switch (protocol.header.code)
            {
                case HhhHelper.Code.HEARTBEAT:
                    break;
                case HhhHelper.Code.SERVER_INFO_SUCCESS:
                    AAServerInfoResponse serverinfo = (AAServerInfoResponse)AAHelper.ByteToStructure(protocol.data, typeof(AAServerInfoResponse));
                    agent.roomCount = serverinfo.roomCount;
                    agent.userCount = serverinfo.userCount;
                    agent.alive = serverinfo.alive;
                    break;

                case HhhHelper.Code.RANKINGS_SUCCESS:
                    userList.RemoveRange(0, userList.Count);
                    UserHandle[] ranking = (UserHandle[])AAHelper.ByteToRanking(protocol.data, typeof(UserHandle));
                    foreach (UserHandle rank in ranking)
                    {
                        userList.Add(rank);
                    }
                    break;

                default:
                    break;
            }
        }

        private void PrintState()
        {
            while (true)
            {
                Console.Clear();
                switch (myState)
                {
                    #region case AdminState.Rank:
                    case AdminState.USERSTATE:
                        if(agentList.Count>0)
                            agentList[0].SendMSG(HhhHelper.Code.RANKINGS);
                        Console.WriteLine("+----------------------------------------+");
                        Console.WriteLine("|  {0,5} |   {1,12} |   {2,10} |", "Rnak", "ID", "Count");
                        Console.WriteLine("+----------------------------------------+");

                        for (int i = 0; i < userList.Count; i++)
                        {
                            Console.WriteLine("|   {0,4} |   {1,12} |   {2,10} |", userList[i].Rank, new string(userList[i].ID), userList[i].MSGCOUNT);
                        }

                        Console.WriteLine("+----------------------------------------+");
                        Console.WriteLine("|#   1. USERSTATE     2. SERVERSTATE    #|");
                        Console.WriteLine("+----------------------------------------+\n\n");
                        Console.Write(userinput);

                        break;
                    #endregion
                    #region case AdminState.ServerState:
                    case AdminState.SERVERSTATE:
                        ServerinfoRequest();
                        Console.WriteLine("+---------------------------------------------------------------------------------+");
                        Console.WriteLine("|   {0,-5} |    {1,-5} |     {2,15} |   {3,-10} |   {4,-10} |  {5,-5} |", "Num", "Type", "Server", "RoomCount", "UserCount", "alive");
                        Console.WriteLine("+---------------------------------------------------------------------------------+");

                        for (int i = 0; i < agentList.Count; i++)
                        {
                            Console.WriteLine("|   {0,5:###} |    {1,5:###} |   {2,15} |   {3,10:##0} |   {4,10:##0} |  {5,5} |", i + 1, agentList[i].type, agentList[i].adminSock.RemoteEndPoint, agentList[i].roomCount, agentList[i].userCount, agentList[i].alive);
                        }
                        Console.WriteLine("+---------------------------------------------------------------------------------+");
                        Console.WriteLine("|#       1. USERSTATE                                                            #|");
                        Console.WriteLine("|#       2. SERVER_START      3. SERVER_RESTART       4. SERVER_STOP             #|");
                        Console.WriteLine("+---------------------------------------------------------------------------------+\n\n");
                        Console.Write(userinput);
                        break;
                        #endregion
                }
                Thread.Sleep(500);
            }
        }

        private void ServerinfoRequest()
        {
            for (int i = 0; i < agentList.Count; i++)
            {
                agentList[i].SendMSG(HhhHelper.Code.SERVER_INFO);
            }
        }
        private void DisConnection(AgentControl agn)
        {
            agentinfo temp = new agentinfo(agn.type, agn.myIPEP);
            Log(agn.myIPEP.ToString() + " disconnetion");
            lock (agentinfoList)
            {
                if (!agentinfoList.Contains(temp))
                    agentinfoList.Add(temp);
                agentList.Remove(agn);
            }
            agn = default(AgentControl);

        }

        private void OnConnection(AgentControl agn)
        {
            Log(agn.myIPEP+" connection");
            lock (agentinfoList)
            {
                if (!agentList.Contains(agn))
                    agentList.Add(agn);
            }
        }

        private async void InputProcess()
        {
            while (true)
            {
                await Task.Run(() => Keyinput());
            }
        }

        private void Keyinput()
        {
            ConsoleKeyInfo input = Console.ReadKey();

            if (input.Key == ConsoleKey.Enter)
            {
                CommandProcess();
                return;
            }
            else if (input.Key == ConsoleKey.Backspace)
            {
                if(userinput.Length>5)
                    userinput.Remove(userinput.Length - 1, 1);
                return;
            }
            userinput.Append(input.KeyChar);
        }

        private void CommandProcess()
        {
            userinput.Remove(0, 5);                     //remove ">> : "
            switch (userinput.ToString().ToUpper())
            {
                case "USERSTATE":
                case "SERVERSTATE":
                    myState = (AdminState)Enum.Parse(typeof(AdminState), userinput.ToString().ToUpper());
                    userinput.Length = 0;
                    userinput.Append(">> : ");
                    break;

                case "SERVER_STOP":
                case "SERVER_START":
                case "SERVER_RESTART":
                    if (myState != AdminState.SERVERSTATE)
                    {
                        userinput.Length = 0;
                        userinput.Append(">> : ");
                        break;
                    }
                    command = (CommnadTYPE)Enum.Parse(typeof(CommnadTYPE), userinput.ToString().ToUpper());
                    userinput.Length = 0;
                    userinput.Append("NUM :");
                    break;

                default:
                    if (Int32.TryParse(userinput.ToString(), out serverNUM))
                    {
                        if (command != 0 && serverNUM > 0 && serverNUM <= agentList.Count)
                        {
                            agentList[serverNUM - 1].SendMSG((ushort)command);
                            Log(agentList[serverNUM - 1].myIPEP + " "+ command.ToString());
                        }
                        command = CommnadTYPE.None;
                    }
                    userinput.Length = 0;
                    userinput.Append(">> : ");
                    break;
            }
        }
    }
}