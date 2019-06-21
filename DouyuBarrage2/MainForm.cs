using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.Net;

using Microsoft.Win32;
using DouyuBarrage3;
using System.IO;
using System.Diagnostics;
//自己写的cmd执行回显模块(C++)
using System.Runtime.InteropServices;
namespace DouyuBarrage2
{
    public partial class MainForm : Form
    {
        #region 相关变量
        private static string barrageServerAddress = "openbarrage.douyutv.com";//斗鱼弹幕服务器地址
        private static int barrageServerPort = 8601;//斗鱼弹幕服务器端
        private delegate void FlushClient();

        private string txt;//保存弹幕信息
        private int level;//用户等级
        private string iconPath;//头像地址
        private string html;
        private string barrageName;
        private static int maxBarrageNum = 200;
        private int currBarrageNum = 0;
        string strdatetime;
        #endregion
        /*
         * 目前解决C#不允许跨线程操纵空间的解决方案是
         * 设置全局的标识变量,用以标识是否需要更新文本局或者调用gdi绘制图像
         * 在一个线程中死循环检测改变标识变量,然后进行更新操作,同时将标识变量设为false
         * 目前存在的问题是,容易出现同时需要更新多个信息,但是线程来不及的问题
         * 解决思路是维护一个list,一直监测list长度
         * 
         *
         */
        [DllImport("RunCmdDll.dll", EntryPoint = "RunCmd")]
        extern static void RunCmd(string command, ref byte result);
        public MainForm()
        {
            SetWebBrowserFeatures(11);
            InitializeComponent();
        }

        public static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            string strip = Dns.GetHostAddresses("openbarrage.douyutv.com")[0].ToString();
            strdatetime = GetTimeStamp();
            Socket clientSocket;
            IPAddress ip = IPAddress.Parse(strip);
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string roomid = "3567314";//"288016";//test
            try
            {
                clientSocket.ReceiveTimeout = 1000000;
                clientSocket.Connect(new IPEndPoint(ip, 8601));
                if (clientSocket.Connected)
                {
                    Console.WriteLine("已与弹幕服务器建立连接");
                }

                MsgBody loginreq = new MsgBody("type@=loginreq/roomid@=" + roomid + "/" + "ver@=20190530/ct@=0");//最后添加的字符串是在2019.5各大视频平台弹幕整改的情况下，使本弹幕接口仍然能够获取到弹幕消息
                //MsgBody send = new MsgBody("type@=joingroup/rid@=" + "22222" + "/gid@=-9999/");
                byte[] msg = loginreq.toByte();
                //byte[] join = send.toByte();
                clientSocket.Send(msg);
                //Thread.Sleep(500);
                MsgBody send = new MsgBody("type@=joingroup/rid@=" + roomid + "/gid@=-9999/");
                byte[] join = send.toByte();
                clientSocket.Send(join);
                //clientSocket.Send(join);

                //Console.WriteLine("消息长度为:" + msg.Length);
                //Console.WriteLine("消息长度为:" + join.Length);

            }
            catch (Exception ex)
            {
                Console.WriteLine("连接服务器失败，请按回车键退出！");
                return;
            }
            //RecvMsg(clientSocket);

            Thread threadRecvMsg = new Thread(RecvMsg);
            threadRecvMsg.IsBackground = true;
            threadRecvMsg.Start(clientSocket);
            Thread threadSendHeartBeat = new Thread(SendHeartBeat);
            threadSendHeartBeat.IsBackground = true;
            threadSendHeartBeat.Start(clientSocket);/*
            Thread threadUpdate = new Thread(updateText);
            threadUpdate.IsBackground = true;
            threadUpdate.Start();*/
        }
        /*
        private void CrossThreadFlush()
        {
            FlushClient fc = new FlushClient(updateTextbox);
            fc.BeginInvoke(null, null);
        }
        private void updateTextbox()
        {
            while (true)
            {
                //this.textBox1.Text += DateTime.Now.ToString();
                Thread.Sleep(1000);
            }
        }*/
        /// <summary>
        /// 线程中接收消息
        /// </summary>
        /// <param name="socket"></param>
        private void RecvMsg(object socket)
        {
            //保存不完整的数据
            byte[] saveArr = new byte[0];
            //每次接收到的数据
            byte[] result = new byte[1024];
            Dictionary<string, string> lastDic = new Dictionary<string, string>();
            Socket clientsocket = (Socket)socket;
            while (true)
            {
                //Console.WriteLine("test");
                //try
                {
                    int receiveLength = ((Socket)socket).Receive(result);
                    if (receiveLength > 0)
                    {
                        if (saveArr.Length > 0)
                        {
                            byte[] arr = new byte[saveArr.Length + receiveLength];
                            Array.Copy(saveArr, 0, arr, 0, saveArr.Length);
                            Array.Copy(result, 0, arr, saveArr.Length, receiveLength);
                            result = arr;
                            receiveLength = result.Length;
                        }
                        int head = 0;
                        if (result.Length < 4)
                        {
                            saveArr = new byte[receiveLength];
                            Array.Copy(result, 0, saveArr, 0, saveArr.Length);
                            continue;
                        }

                        byte[] headbytes = new byte[4];
                        Array.Copy(result, 0, headbytes, 0, 4);
                        head = BitConverter.ToInt32(headbytes, 0);
                        //head = BitConverter.ToInt32(SubByte(result, 0, 4), 0);

                        string str = string.Empty;
                        if (head > (receiveLength - 4))
                        {
                            saveArr = new byte[result.Length];
                            Array.Copy(result, 0, saveArr, 0, saveArr.Length);
                            continue;
                        }
                        int index = 0;
                        do
                        {
                            byte[] test = new byte[result.Length - 12];
                            Array.Copy(result, 12, test, 0, result.Length - 12);
                            string str1 = Encoding.UTF8.GetString(test);

                            STTDecoder decoder = new STTDecoder(str1);
                            decoder.process();
                            Dictionary<string, string> dic = decoder.getKeys();
                            if (dic.Keys.Contains("type") && dic["type"] == "chatmsg")
                            {
                                txt = dic["txt"];
                                //Console.WriteLine(txt);
                                level = Int32.Parse(dic["level"]);
                                iconPath = dic["ic"];
                                barrageName = dic["nn"];
                                /*
                                Action<string, int, string, string> updateAction = new Action<string, int, string, string>(change);
                                updateAction.BeginInvoke(txt, level, iconPath, barrageName, null, null) ;
                                updateAction(txt, level, iconPath, barrageName);*/

                                MethodInvoker ln = new MethodInvoker(change);
                                if (this.IsHandleCreated)
                                {
                                    this.BeginInvoke(ln, null);
                                }
                                using (StreamWriter sw = new StreamWriter("barrages" + strdatetime +".txt",true))
                                {
   
                                    sw.WriteLine(txt);

                                }
                                Console.WriteLine(txt);
                                //Console.WriteLine(cid);
                                //MessageBox.Show(txt);
                                Thread.Sleep(100);
                                // dic.Clear();
                            }
                            else if (dic.Keys.Contains("type") && dic["type"] == "uenter")
                            {
                                barrageName = dic["nn"];
                                MethodInvoker ln = new MethodInvoker(updateUserCome);
                                if (this.IsHandleCreated)
                                {
                                    this.BeginInvoke(ln, null);
                                }
                                
                                //MessageBox.Show("有人来了");
                            }



                            index = head + 4;

                            if (index < receiveLength)
                            {
                                try
                                {
                                    //result = SubByte(result, index, receiveLength - index);
                                    byte[] result2 = new byte[receiveLength - index];
                                    Array.Copy(result, index, result2, 0, receiveLength - index);
                                    result = result2;
                                    if (result.Length <= 12)
                                    {
                                        saveArr = new byte[result.Length];
                                        Array.Copy(result, 0, saveArr, 0, saveArr.Length);
                                        break;
                                    }
                                    headbytes = new byte[4];
                                    Array.Copy(result, 0, headbytes, 0, 4);
                                    head = BitConverter.ToInt32(headbytes, 0);
                                    if (result.Length < (head + 4))
                                    {
                                        saveArr = new byte[result.Length];
                                        Array.Copy(result, 0, saveArr, 0, saveArr.Length);
                                        break;
                                    }
                                    receiveLength = result.Length;
                                    continue;
                                }
                                catch (Exception ex)
                                {

                                    throw;
                                }
                            }
                            saveArr = new byte[0];
                            break;
                        } while (true);



                        //Console.WriteLine("Head:" + head + " result:" + result.Length);
                    }



                    //this.textBox1.Text += "55";
                    //Console.WriteLine(str1);

                }/*
                catch (Exception ex)
                {
                    throw;
                    //Console.WriteLine("E Called");
                }*/

            }
        }
        void updateUserCome()
        {
            textBox1.Text += barrageName + "进入了直播间\r\n";
        }
        //void change(string txt, int level, string iconPath, string barrageName)
        void change()
        {
            //Console.WriteLine("Called");
            currBarrageNum++;
            if (currBarrageNum > maxBarrageNum)
            {
                currBarrageNum = 0;
                html = "";
            }
           // textBox1.Text += txt + "\r\n";
            //Console.WriteLine(level);
            //Console.WriteLine(iconPath);
            //<img style='border-radius: 25px;border: none;vertical-align:middle' width='30px' height='30px' src='http://apic.douyucdn.cn/upload/" + iconPath + "_small.jpg'/>
            html = "<div>" + "<font face='微软雅黑' color='#42b2b5'>&nbsp;" + barrageName + "</font>" + "&nbsp;<img style='vertical-align:middle' height='15' width ='40' src='https://staticlive.douyucdn.cn/common/douyu/images/userViewUserLevel/m2_" + level + ".png'/>&nbsp;" + "<font style='vertical-align:middle' face='微软雅黑' color='rgb(153, 176, 208)'>" + txt + "</font></div>" + html; ;
            //Console.WriteLine(html);
            //if (barrageDisplayer.InvokeRequired)
           
            barrageDisplayer.DocumentText = "<html><body style='background:rgb(34,52,90)'>" + html + "</body></html>";
                //textBox1.Text = txt + "\r\n" + textBox1.Text;

            

        }



        /// <summary>
        /// 每隔45s发送心跳消息
        /// </summary>
        static void SendHeartBeat(object socket)
        {
            while (true)
            {
                Socket clientsocket = (Socket)socket;
                MsgBody body = new MsgBody("type@=mrkl/");
                byte[] msg = body.toByte();
                clientsocket.Send(msg);
                Thread.Sleep(15000);
                //MessageBox.Show("" + clientsocket.Connected.ToString());
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
        //关闭窗体释放资源前关闭tcp连接
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void barrageDisplayer_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            //barrageDisplayer.Opacity = 1;
        }
        /// <summary>  
        /// 修改注册表信息来兼容当前程序  
        ///   
        /// </summary>  
        static void SetWebBrowserFeatures(int ieVersion)
        {
            // don't change the registry if running in-proc inside Visual Studio  
            if (LicenseManager.UsageMode != LicenseUsageMode.Runtime)
                return;
            //获取程序及名称  
            var appName = System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            //得到浏览器的模式的值  
            UInt32 ieMode = GeoEmulationModee(ieVersion);
            var featureControlRegKey = @"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\";
            //设置浏览器对应用程序（appName）以什么模式（ieMode）运行  
            Registry.SetValue(featureControlRegKey + "FEATURE_BROWSER_EMULATION",
                appName, ieMode, RegistryValueKind.DWord);
            // enable the features which are "On" for the full Internet Explorer browser  
            //不晓得设置有什么用  
            Registry.SetValue(featureControlRegKey + "FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION",
                appName, 1, RegistryValueKind.DWord);


            //Registry.SetValue(featureControlRegKey + "FEATURE_AJAX_CONNECTIONEVENTS",  
            //    appName, 1, RegistryValueKind.DWord);  


            //Registry.SetValue(featureControlRegKey + "FEATURE_GPU_RENDERING",  
            //    appName, 1, RegistryValueKind.DWord);  


            //Registry.SetValue(featureControlRegKey + "FEATURE_WEBOC_DOCUMENT_ZOOM",  
            //    appName, 1, RegistryValueKind.DWord);  


            //Registry.SetValue(featureControlRegKey + "FEATURE_NINPUT_LEGACYMODE",  
            //    appName, 0, RegistryValueKind.DWord);  
        }
        /// <summary>  
        /// 获取浏览器的版本  
        /// </summary>  
        /// <returns></returns>  
        static int GetBrowserVersion()
        {
            int browserVersion = 0;
            using (var ieKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer",
                RegistryKeyPermissionCheck.ReadSubTree,
                System.Security.AccessControl.RegistryRights.QueryValues))
            {
                var version = ieKey.GetValue("svcVersion");
                if (null == version)
                {
                    version = ieKey.GetValue("Version");
                    if (null == version)
                        throw new ApplicationException("Microsoft Internet Explorer is required!");
                }
                int.TryParse(version.ToString().Split('.')[0], out browserVersion);
            }
            //如果小于7  
            if (browserVersion < 7)
            {
                throw new ApplicationException("不支持的浏览器版本!");
            }
            return browserVersion;
        }
        /// <summary>  
        /// 通过版本得到浏览器模式的值  
        /// </summary>  
        /// <param name="browserVersion"></param>  
        /// <returns></returns>  
        static UInt32 GeoEmulationModee(int browserVersion)
        {
            UInt32 mode = 11000; // Internet Explorer 11. Webpages containing standards-based !DOCTYPE directives are displayed in IE11 Standards mode.   
            switch (browserVersion)
            {
                case 7:
                    mode = 7000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE7 Standards mode.   
                    break;
                case 8:
                    mode = 8000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE8 mode.   
                    break;
                case 9:
                    mode = 9000; // Internet Explorer 9. Webpages containing standards-based !DOCTYPE directives are displayed in IE9 mode.                      
                    break;
                case 10:
                    mode = 10000; // Internet Explorer 10.  
                    break;
                case 11:
                    mode = 11000; // Internet Explorer 11  
                    break;
            }
            return mode;
        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

            listView1.Clear();
            this.listView1.Columns.Add("高频词", 120, HorizontalAlignment.Left);//一步添加
            this.listView1.Columns.Add("频次", 120, HorizontalAlignment.Left);//一步添加
            byte[] result = new byte[10240];
            RunCmd("python E:/Projects/新建文件夹/RemoteControl/DouyuBarrage2/DouyuBarrage2/bin/Debug/词频分析.py " + "barrages" + strdatetime + ".txt", ref result[0]);
            string strresult = System.Text.Encoding.Default.GetString(result, 0, result.Length).TrimEnd('\0');
            string[] perstr = strresult.Split('\n');
            listView1.BeginUpdate();
            foreach (string d in perstr)
            {
                //MessageBox.Show(d);   
                if (d.Contains(" "))
                {
                    string[] group = d.Split(' ');
                    ListViewItem item = new ListViewItem();
                    item.Text = group[0];
                    item.SubItems.Add(group[1]);
                    listView1.Items.Add(item);
                }

                
            }
            listView1.EndUpdate();
            
        }


    }
}
