using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Net;
using System.Net.Sockets;

using SocketGlobal;

namespace SocketClient
{
    public partial class frmSocketClient : Form
    {
        /// 명령어 클래스
        private claCommand m_insCommand = new claCommand();

        /// 내 소켓
        private Socket m_socketMe = null;

        /// 나의 상태
        enum typeState
        {
            /// 없음
            None = 0,
            /// 연결중
            Connecting,
            /// 연결 완료
            Connect,
        }
        /// 로그인 
        private bool m_bLogin = false;

        /// 나의 상태
        private typeState m_typeState = typeState.None;

        public frmSocketClient()
        {
            InitializeComponent();

            //유아이 기본으로 세팅
            UI_Setting(typeState.None);
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            switch (m_typeState)
            {
                case typeState.None:    //기본
                    if ("" == txtMsg.Text)
                    {
                        //입력값이 없으면 리턴
                        MessageBox.Show("아이디를 넣고 시도해 주세요");
                        return;
                    }
                    else
                    {
                        //아이디가 있으면 로그인 시작

                        //유아이를 세팅하고
                        UI_Setting(typeState.Connecting);

                        //소켓 생성
                        Socket socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        IPEndPoint ipepServer = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Convert.ToInt32(txtPort.Text));

                        SocketAsyncEventArgs saeaServer = new SocketAsyncEventArgs();
                        saeaServer.RemoteEndPoint = ipepServer;
                        //연결 완료 이벤트 연결
                        saeaServer.Completed += new EventHandler<SocketAsyncEventArgs>(Connect_Completed);

                        //서버 메시지 대기
                        socketServer.ConnectAsync(saeaServer);

                    }
                    break;

                case typeState.Connect: //접속 상태
                                        //이상태에서는 메시지를 보낸다.
                    StringBuilder sbData = new StringBuilder();
                    sbData.Append(claCommand.Command.Msg.GetHashCode());
                    sbData.Append(claGlobal.g_Division);
                    sbData.Append(txtMsg.Text);
                    SendMsg(sbData.ToString());
                    txtMsg.Text = "";
                    break;
            }
        }

        /// <summary>
        /// 연결 완료
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Connect_Completed(object sender, SocketAsyncEventArgs e)
        {
            m_socketMe = (Socket)sender;

            if (true == m_socketMe.Connected)
            {
                MessageData mdReceiveMsg = new MessageData();

                //서버에 보낼 객체를 만든다.
                SocketAsyncEventArgs saeaReceiveArgs = new SocketAsyncEventArgs();
                //보낼 데이터를 설정하고
                saeaReceiveArgs.UserToken = mdReceiveMsg;
                //데이터 길이 세팅
                saeaReceiveArgs.SetBuffer(mdReceiveMsg.GetBuffer(), 0, 4);
                //받음 완료 이벤트 연결
                saeaReceiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(Recieve_Completed);
                //받음 보냄
                m_socketMe.ReceiveAsync(saeaReceiveArgs);

                DisplayMsg("*** 서버 연결 성공 ***");
                //서버 연결이 성공하면 id체크를 시작한다.
                Login();
            }
            else
            {
                Disconnection();
            }
        }

        private void Recieve_Completed(object sender, SocketAsyncEventArgs e)
        {
            Socket socketClient = (Socket)sender;
            MessageData mdRecieveMsg = (MessageData)e.UserToken;
            mdRecieveMsg.SetLength(e.Buffer);
            mdRecieveMsg.InitData();

            if (true == socketClient.Connected)
            {
                //연결이 되어 있다.

                //데이터 수신
                socketClient.Receive(mdRecieveMsg.Data, mdRecieveMsg.DataLength, SocketFlags.None);

                //명령을 분석 한다.
                MsgAnalysis(mdRecieveMsg.GetData());

                //다음 메시지를 받을 준비를 한다.
                socketClient.ReceiveAsync(e);
            }
            else
            {
                Disconnection();
            }
        }

        /// <summary>
        /// UI 세팅
        /// </summary>
        /// <param name="typeSet"></param>
        private void UI_Setting(typeState typeSet)
        {
            //들어온 값을 세팅하고
            m_typeState = typeSet;

            switch (typeSet)
            {
                case typeState.None:    //기본
                    btnSend.Text = "로그인";
                    break;
                case typeState.Connecting:  //연결중
                    txtMsg.Enabled = false;
                    btnSend.Text = "연결중";
                    btnSend.Enabled = false;
                    break;
                case typeState.Connect: //연결완료
                    this.Invoke(new Action(
                        delegate ()
                        {
                            txtMsg.Enabled = true;
                            btnSend.Text = "보내기";
                            btnSend.Enabled = true;
                        }));
                    break;
            }
        }


        /// <summary>
        /// 넘어온 데이터를 분석 한다.
        /// </summary>
        /// <param name="sMsg"></param>
        private void MsgAnalysis(string sMsg)
        {
            //구분자로 명령을 구분 한다.
            string[] sData = sMsg.Split(claGlobal.g_Division);

            //데이터 개수 확인
            if ((1 <= sData.Length))
            {
                //0이면 빈메시지이기 때문에 별도의 처리는 없다.

                //넘어온 명령
                claCommand.Command typeCommand
                    = m_insCommand.StrIntToType(sData[0]);

                switch (typeCommand)
                {
                    case claCommand.Command.None:   //없다
                        break;
                    case claCommand.Command.Msg:    //메시지인 경우
                        Command_Msg(sData[1]);
                        break;
                    case claCommand.Command.ID_Check_Ok:    //아이디 성공
                        SendMeg_IDCheck_Ok();
                        break;
                    case claCommand.Command.ID_Check_Fail:  //아이디 실패
                        SendMeg_IDCheck_Fail();
                        break;
                    case claCommand.Command.User_Connect:   //다른 유저가 접속 했다.
                        SendMeg_User_Connect(sData[1]);
                        break;
                    case claCommand.Command.User_Disonnect: //다른 유저가 접속을 끊었다.
                        SendMeg_User_Disconnect(sData[1]);
                        break;
                    case claCommand.Command.User_List:  //유저 리스트 갱신
                        SendMeg_User_List(sData[1]);
                        break;
                }
            }
        }

        /// <summary>
        /// 메시지 출력
        /// </summary>
        /// <param name="sMsg"></param>
        private void Command_Msg(string sMsg)
        {
            DisplayMsg(sMsg);
        }

        /// <summary>
        /// 아이디 성공
        /// </summary>
        private void SendMeg_IDCheck_Ok()
        {
            this.Invoke(new Action(
                        delegate ()
                        {
                            labID.Text = txtMsg.Text;
                            txtMsg.Text = "";
                        }));

            //UI갱신
            UI_Setting(typeState.Connect);

            //아이디확인이 되었으면 서버에 로그인 요청을 하여 로그인을 끝낸다.
            StringBuilder sbData = new StringBuilder();
            sbData.Append(claCommand.Command.Login.GetHashCode());
            sbData.Append(claGlobal.g_Division);

            SendMsg(sbData.ToString());
        }

        /// <summary>
        /// 아이디체크 실패
        /// </summary>
        private void SendMeg_IDCheck_Fail()
        {
            DisplayMsg("로그인 실패 : 다른 아이디를 이용해 주세요.");
            //연결 끊기
            Disconnection();
        }

        /// <summary>
        /// 접속한 유저가 있다.
        /// </summary>
        private void SendMeg_User_Connect(string sUserID)
        {
            this.Invoke(new Action(
                        delegate ()
                        {
                            listUser.Items.Add(sUserID);
                        }));
        }

        /// <summary>
        /// 접속을 끊은 유저가 있다.
        /// </summary>
        /// <param name="sUserID"></param>
        private void SendMeg_User_Disconnect(string sUserID)
        {
            this.Invoke(new Action(
                        delegate ()
                        {
                            listUser.Items.RemoveAt(listUser.FindString(sUserID));
                        }));
        }

        /// <summary>
        /// 유저리스트 
        /// </summary>
        /// <param name="sUserList"></param>
        private void SendMeg_User_List(string sUserList)
        {
            this.Invoke(new Action(
                delegate ()
                {
                    //리스트를 비우고
                    listUser.Items.Clear();

                    //리스트를 다시 채워준다.
                    string[] sList = sUserList.Split(',');
                    for (int i = 0; i < sList.Length; ++i)
                    {
                        listUser.Items.Add(sList[i]);
                    }

                }));
        }

        /// <summary>
        /// 받아온 메시지를 출력 한다.
        /// </summary>
        /// <param name="nMessage"></param>
        /// <param name="nType"></param>
        private void DisplayMsg(String nMessage)
        {
            StringBuilder buffer = new StringBuilder();

            //출력할 메시지 완성
            buffer.Append(nMessage);

            //출력
            this.Invoke(new Action(
                        delegate ()
                        {
                            listMsg.Items.Add(nMessage);
                        }));

        }

        /// <summary>
        /// 접속이 끊겼다.
        /// </summary>
        private void Disconnection()
        {
            //접속 끊김
            m_socketMe = null;

            //유아이를 세팅하고
            UI_Setting(typeState.None);

            DisplayMsg("*** 서버 연결 끊김 ***");
        }

        /// <summary>
        /// 아이디 체크 요청
        /// </summary>
        private void Login()
        {
            StringBuilder sbData = new StringBuilder();
            sbData.Append(claCommand.Command.ID_Check.GetHashCode());
            sbData.Append(claGlobal.g_Division);
            sbData.Append(txtMsg.Text);

            SendMsg(sbData.ToString());
        }

        private void Logout()
        {
            StringBuilder sbData = new StringBuilder();
            sbData.Append(claCommand.Command.Logout.GetHashCode());
            sbData.Append(claGlobal.g_Division);

            SendMsg(sbData.ToString());
        }

        /// <summary>
        /// 서버로 메시지를 전달 합니다.
        /// </summary>
        /// <param name="sMsg"></param>
        private void SendMsg(string sMsg)
        {
            MessageData mdSendMsg = new MessageData();

            //데이터를 넣고
            mdSendMsg.SetData(sMsg);

            //서버에 보낼 객체를 만든다.
            SocketAsyncEventArgs saeaServer = new SocketAsyncEventArgs();
            //데이터 길이 세팅
            saeaServer.SetBuffer(BitConverter.GetBytes(mdSendMsg.DataLength), 0, 4);
            //보내기 완료 이벤트 연결
            saeaServer.Completed += new EventHandler<SocketAsyncEventArgs>(Send_Completed);
            //보낼 데이터 설정
            saeaServer.UserToken = mdSendMsg;
            //보내기
            m_socketMe.SendAsync(saeaServer);
        }

        /// <summary>
        /// 메시지 보내기 완료
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Send_Completed(object sender, SocketAsyncEventArgs e)
        {
            Socket socketSend = (Socket)sender;
            MessageData mdMsg = (MessageData)e.UserToken;
            //데이터 보내기 마무리
            socketSend.Send(mdMsg.Data);
        }

        /// <summary>
        /// 테스트용 자동 메시지 타이머
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            StringBuilder sbData = new StringBuilder();
            sbData.Append(claCommand.Command.Msg.GetHashCode());
            sbData.Append(claGlobal.g_Division);
            sbData.Append(txtAutoMsg.Text);
            SendMsg(sbData.ToString());

        }

        /// <summary>
        /// 테스트용 자동 메시지 타이머 제어용 버튼
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (true == timer1.Enabled)
            {
                timer1.Enabled = false;
            }
            else
            {
                timer1.Enabled = true;
            }
        }

    }
}
