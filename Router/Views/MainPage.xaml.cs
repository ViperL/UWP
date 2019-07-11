using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Router.Model;
using Router.Controller;
using System.Net;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Networking.Connectivity;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using System.Threading;

using Windows.Media.SpeechRecognition;//语音识别
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.ApplicationModel;
using System.Diagnostics;

using Windows.UI.ViewManagement;//状态栏
using Windows.Security.ExchangeActiveSyncProvisioning;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace Router
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region 列表对象
        List<DeviceModel> Obj = new List<DeviceModel>();//已注册的设备列表
        List<DeviceModel> ListLinkRequst = new List<DeviceModel>();//请求连接的设备列表
        #endregion

        #region 调用对象
        DatagramSocket socket = null;//套接字实体对象
        private Timer periodicTimer;
        static Router.Controller.FileIO FileMethod = new Router.Controller.FileIO("DataQuene.line");//创建本地数据队列
        #endregion

        #region 属性对象
        string LocIp;
        string oldStr = "";//用于过滤重复信息
        bool FanFlag = false;//风扇信号标志
        static bool RSAFlag = false;//是否使用RSA加密
        static bool AZureFlag = false;//是否使用RSA加密
        #endregion

        #region 串口/ZigBee对象
        private SerialDevice serialPort = null;
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;
        private ObservableCollection<DeviceInformation> listOfDevices;
        private CancellationTokenSource ReadCancellationTokenSource;
        private List<DatagramSocket> ListSocket = new List<DatagramSocket>();//用于存储绑定的串口

        #endregion



        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
            Unloaded += MainPage_Unloaded;
            listOfDevices = new ObservableCollection<DeviceInformation>();//初始化串口列表
            ListAvailablePorts();//检查可用串口
            if(GetDeviceInfo()== "WINDOWS")//如果是PC开启语音--IOT设备识别号为WINDOWS IOT
                SoundListen();
        }

        public async void Invoke(Action action, Windows.UI.Core.CoreDispatcherPriority Priority = Windows.UI.Core.CoreDispatcherPriority.Normal)//跨线程
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Priority, () => { action(); });
        }

        private string GetDeviceInfo()//获取设备信息
        {
            EasClientDeviceInformation deviceInfo = new EasClientDeviceInformation();
            return deviceInfo.SystemProductName;
        }

        #region 网络相关

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            socket = new DatagramSocket();
            socket.Control.MulticastOnly = true;
            socket.MessageReceived += Socket_MessageReceived;
            await socket.BindServiceNameAsync("9000");
            #region 获取本地IP
            var hosts = NetworkInformation.GetHostNames();
            var host = hosts.FirstOrDefault(h =>
            {
                bool isIpaddr = (h.Type == Windows.Networking.HostNameType.Ipv4) || (h.Type == Windows.Networking.HostNameType.Ipv6);
                if (isIpaddr == false)
                {
                    return false;
                }
                IPInformation ipinfo = h.IPInformation;
                if (ipinfo.NetworkAdapter.IanaInterfaceType == 71 || ipinfo.NetworkAdapter.IanaInterfaceType == 6)
                {
                    return true;
                }
                return false;
            });
            if (host != null)
            {
                LocIp =host.DisplayName;
                txtLocIp.Text = "本机IP:" + host.DisplayName; //显示IP
            }
            #endregion
            #region 布局表格
            if (await DevicesMethod.IsFirstUse() == true)
                DevicesMethod.CreateLog();//设备文件丢失
            Obj = await DevicesMethod.GetDevices();
            List<ViewDevModel> ObjView = new List<ViewDevModel>();
            foreach (var item in Obj)
            {
                ObjView.Add(ModelConverter.DataToView(item));
            }
            Controller.GridViewMethod.LayOutItem(gridView, ObjView);//布置控件
            if (await IsFirstUse() == true)//队列文件丢失
                FileMethod.CreateTxt();
            #endregion
            foreach (var item in Obj)//布置已注册端口
            {
                CreateLine(item.NetPort);
            }
            lblNetWorkName.Text = "  "+InternetStatus.GetNetWorkName();//获取网络名称
            //StatusBar S = StatusBar.GetForCurrentView();
            //await S.HideAsync();
        }

        private async void CreateLine(int Port)//绑定新通道
        {
            try
            {
                socket = new DatagramSocket();
                socket.Control.MulticastOnly = true;
                socket.MessageReceived += Socket_MessageReceived1;
                await socket.BindServiceNameAsync(Port.ToString());
                ListSocket.Add(socket);
            }
            catch (Exception)
            { }
        }

        private void DisposeLine(int Port)
        {
            for (int i = 0; i < ListSocket.Count; i++)
            {
                if (ListSocket[i].Information.LocalPort == Port.ToString())
                {
                    ListSocket[i].Dispose();//销毁对象
                    ListSocket.Remove(ListSocket[i]);
                }
            }
        }//移除监听

        private void Socket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            string remoteaddr = args.RemoteAddress.DisplayName;//远端IP地址
            string locaddress = args.LocalAddress.DisplayName;
            string remoteport = args.RemotePort;
            bool Flag = false;//标志
            DataReader reader = args.GetDataReader();
            reader.UnicodeEncoding = UnicodeEncoding.Utf8;// 读长度
            uint len = reader.ReadUInt32();// 读内容
            string msg = reader.ReadString(reader.UnconsumedBufferLength);
            foreach (var item in ListLinkRequst)
            {
                if (item.IPAddress == remoteaddr)
                    Flag = true;//重复请求--忽略
            }
            if (Flag == false)
                ListLinkRequst.Add(new DeviceModel { IPAddress = remoteaddr, Type = msg });//添加请求列表
        }//请求通道

        private void Socket_MessageReceived1(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            string remoteaddr = args.RemoteAddress.DisplayName;//远端IP地址
            if (remoteaddr == LocIp)
                return;//本机IP不做处理
            DataReader reader = args.GetDataReader();
            reader.UnicodeEncoding = UnicodeEncoding.Utf8;// 读长度
            uint len = reader.ReadUInt32();// 读内容
            string msg = reader.ReadString(reader.UnconsumedBufferLength);
            if (oldStr == msg)
                return;//重复过滤器
            else
                oldStr = msg;
            FileMethod.AddQuene(LocIp+ "#" + remoteaddr + "#" + msg+ "#" + DateTime.Now.ToString());//压入队列
        }//数据通道

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (socket != null)
            {
                socket.MessageReceived -= Socket_MessageReceived;
                socket.Dispose();
                socket = null;
            }
        }//释放资源

        private async static Task<bool> IsFirstUse()
        {
            if (await FileMethod.PoPOut() == null)
                return true;
            else
                return false;
        }//补充存储文件

        private async void SendAllow(int PortNum)
        {
            string content = PortNum.ToString();//响应端口请求
            if (string.IsNullOrEmpty(content)) return;
            using (DatagramSocket socket = new DatagramSocket())
            {
                HostName broardaddr = new HostName(IPAddress.Broadcast.ToString());
                IOutputStream outstream = await socket.GetOutputStreamAsync(broardaddr, "8999");
                DataWriter writer = new DataWriter(outstream);
                writer.UnicodeEncoding = UnicodeEncoding.Utf8;
                uint len = writer.MeasureString(content);
                writer.WriteUInt32(len);
                writer.WriteString(content);
                await writer.StoreAsync();
                writer.Dispose();
            }
        }//发送端口许可

        private async void SendCmd(int PortNum,string Cmd)
        {
            string content = Cmd;
            if (string.IsNullOrEmpty(content)) return;
            using (DatagramSocket socket = new DatagramSocket())
            {
                HostName broardaddr = new HostName(IPAddress.Broadcast.ToString());
                IOutputStream outstream = await socket.GetOutputStreamAsync(broardaddr, PortNum.ToString());
                DataWriter writer = new DataWriter(outstream);
                writer.UnicodeEncoding = UnicodeEncoding.Utf8;
                uint len = writer.MeasureString(content);
                writer.WriteUInt32(len);
                writer.WriteString(content);
                await writer.StoreAsync();
                writer.Dispose();
            }
        }//发送指令

        private async void UpLoadData(int PortNum, Router.Controller.FileIO Obj)
        {
            try
            {
                string[] DataQune = await Obj.PoPToCloud();//Obj.PoPOut();PoPToCloud();
                foreach (var item in DataQune)
                {
                    string content = "";
                    if (RSAFlag == true)
                        content = RSAConvert.PublicEncrypt(item);//RSA加密
                    else
                        content = item;
                    if (AZureFlag == true)//使用AZure IoT
                    {
                        await AzureIoTHub.SendDeviceToCloudMessageAsync(item);
                    }

                    if (string.IsNullOrEmpty(content)) continue;
                    using (DatagramSocket socket = new DatagramSocket())
                    {
                        HostName broardaddr = new HostName(IPAddress.Broadcast.ToString());
                        IOutputStream outstream = await socket.GetOutputStreamAsync(broardaddr, PortNum.ToString());
                        DataWriter writer = new DataWriter(outstream);
                        writer.UnicodeEncoding = UnicodeEncoding.Utf8;
                        uint len = writer.MeasureString(content);
                        writer.WriteUInt32(len);
                        writer.WriteString(content);
                        await writer.StoreAsync();
                        writer.Dispose();
                    }
                }
            }
            catch (Exception) { }
            
        }//向云上传数据

        #endregion

        #region 设备模型相关

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string[] data = cmbIp.SelectedItem.ToString().Trim().Split(Convert.ToChar("/"));

            DeviceModel Device = new DeviceModel {
                NetPort =await DevicesMethod.GetPortNum()+1,
                Type = data[1],
                IPAddress = data[0]
            };
            ViewDevModel DevObj = ModelConverter.DataToView(Device);
            Obj.Add(Device);
            DevicesMethod.UpdateDevices(Obj);
            gridView.Items.Add(DevObj);
            CreateLine(Device.NetPort);//绑定新通道
            SendAllow(Device.NetPort);//发送端口允许
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            cmbIp.Items.Clear();//清除掉之前的内容
            foreach (var item in ListLinkRequst)
            {
                cmbIp.Items.Add(item.IPAddress + "/" + item.Type);
            }
        }

        private void BtnZigRefreash_Click(object sender, RoutedEventArgs e)
        {
            ListAvailablePorts();
        } //刷新可用串口

        private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           
        }//左键选择，暂时取消

        private async void GridView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var Info = (e.OriginalSource as FrameworkElement)?.DataContext as ViewDevModel;
            DeviceModel DataInfo = ModelConverter.ViewToData(Info);
            ContentDialog detail_dialog = new ContentDialog()
            {
                Title = DataInfo.Type + "设备详情:",
                Content = "设备地址：" + DataInfo.IPAddress + ":" + DataInfo.NetPort,
                PrimaryButtonText = "发送命令",
                SecondaryButtonText = "删除设备",
                CloseButtonText = "返回",
                FullSizeDesired = true,
            };
            ContentDialogResult Reslut = await detail_dialog.ShowAsync();
            if (Reslut == ContentDialogResult.Primary)
            {
                if (DataInfo.IPAddress == "Zigbee设备")//关闭串口服务
                {
                    WriteCmdByte(new byte[] { 0x02 });
                    //SendCmd(Convert.ToInt16("9003"), "0000+B");
                }
                else
                {
                    if (FanFlag == false)
                    {
                        SendCmd(Convert.ToInt16(DataInfo.NetPort), "0000+A");
                        FanFlag = true;
                    }
                    else
                    {
                        SendCmd(Convert.ToInt16(DataInfo.NetPort), "0000+B");
                        FanFlag = false;
                    }
                }
                    //SendCmd(DataInfo.NetPort, "这是一条测试信息/");
            }
            if (Reslut == ContentDialogResult.Secondary)
            {
                if (DataInfo.IPAddress == "Zigbee设备")//关闭串口服务
                {
                    try
                    {
                        CancelReadTask();
                        CloseDevice();
                        ListAvailablePorts();
                    }
                    catch (Exception)
                    { }
                    btnAddZigBee.IsEnabled = true;
                }
                DisposeLine(DataInfo.NetPort);//移除监听服务
                for (int i = 0; i < Obj.Count; i++)
                {
                    if (Obj[i].NetPort == DataInfo.NetPort)
                        Obj.Remove(Obj[i]);
                }
                gridView.Items.Remove(Info);
                DevicesMethod.UpdateDevices(Obj);
            }
        }//右键选择

        #endregion

        #region 云相关

        private async void CreateCmdLine()//建立服务器通道
        {
            try
            {
                socket = new DatagramSocket();
                socket.Control.MulticastOnly = true;
                socket.MessageReceived += Socket_MessageReceived2;
                await socket.BindServiceNameAsync("8000");
            }
            catch (Exception)
            { }
        }

        private async void SendDeviceList(int PortNum, List<DeviceModel> Obj)
        {
            foreach (var item in Obj)
            {
                string content = item.IPAddress+"!"+item.NetPort+"!"+item.Type;
                if (string.IsNullOrEmpty(content)) return;
                using (DatagramSocket socket = new DatagramSocket())
                {
                    HostName broardaddr = new HostName(IPAddress.Broadcast.ToString());
                    IOutputStream outstream = await socket.GetOutputStreamAsync(broardaddr, PortNum.ToString());
                    DataWriter writer = new DataWriter(outstream);
                    writer.UnicodeEncoding = UnicodeEncoding.Utf8;
                    uint len = writer.MeasureString(content);
                    writer.WriteUInt32(len);
                    writer.WriteString(content);
                    await writer.StoreAsync();
                    writer.Dispose();
                }
            }

        }//出栈

        private void Socket_MessageReceived2(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)//与服务器进行通讯 8000端口
        {
            string remoteaddr = args.RemoteAddress.DisplayName;//远端IP地址
            //if (remoteaddr == LocIp)
            //    return;//本机IP不做处理
            DataReader reader = args.GetDataReader();
            reader.UnicodeEncoding = UnicodeEncoding.Utf8;// 读长度
            uint len = reader.ReadUInt32();// 读内容
            string msg = reader.ReadString(reader.UnconsumedBufferLength);
            if (oldStr == msg)
                return;//重复过滤器
            else
                oldStr = msg;
            try
            {
                CmdModel Cmd = CmdAnay.CmdAnayer(msg);
                if (Cmd.PortNum == "8000")//本机指令
                {
                    switch (Cmd.Cmd)
                    {
                        case "DeviceList": SendDeviceList(8001, Obj); break;//请求设备列表命令
                        default: break;
                    }
                }
                else
                {
                    switch (Cmd.Cmd)
                    {
                        case "FanOnOrOff": SendCmd(Convert.ToInt16(Cmd.PortNum), "0000+A"); ; break;
                        case "FanValueSet": SendCmd(Convert.ToInt16(Cmd.PortNum), "0000+B"); ; break;
                        case "Led1OnOrOff": WriteCmdByte(new byte[] { 0x01 }); break;
                        case "Led2OnOrOff": WriteCmdByte(new byte[] { 0x02 }); break;
                        case "LedFlu": WriteCmdByte(new byte[] { 0x03 }); break;
                        default: break;
                    }
                }
            }
            catch (Exception) { }
        }

        private void BtnServceSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (btnServceSwitch.IsOn == true)
            {
                periodicTimer = new Timer(this.TimerCallback, null, 0, Convert.ToInt16(SilderTimeSpan.Value));
                CreateCmdLine();//连接云服务
                SendDeviceList(8001, Obj);
                UpLoadData(8001, FileMethod);
                txtStatue.Visibility = Visibility.Visible;
                ProgressOnLine.IsActive = true;
            }
            else
            {
                periodicTimer.Dispose();//解除定时器
                txtStatue.Visibility = Visibility.Collapsed;
                ProgressOnLine.IsActive = false;
            }
        }

        private void BtnSet_Click(object sender, RoutedEventArgs e)
        {
            if (SplitPage.IsPaneOpen == false)//打开设置同时加载AZure信息
            {
                SplitPage.IsPaneOpen = true;
                //SettingModel ObjPara = new SettingModel();
                //if (await SettingMethod.IsFirstUse() == true)
                //    SettingMethod.CreateSetFile();//如果设置文件丢失则创建设置文件
                //try
                //{
                //    ObjPara = await SettingMethod.GetParameter();
                //    txtAzure.Text = ObjPara.AzureID;
                //    txtPSW.Password = ObjPara.AzurePSW;
                //}
                //catch (Exception) { }
            }
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            try
            {
                lblTimeSpan.Text = SilderTimeSpan.Value.ToString() + "ms";
            }
            catch (Exception) { }
        }

        private void TimerCallback(object state)//定时器调用
        {
            this.Invoke(() =>
            {
                RSAFlag = btnRSASwitch.IsOn;
                AZureFlag = btnAZureSwitch.IsOn;
            });

            
            UpLoadData(8001, FileMethod);
        }
        #endregion

        #region 串口/zigbee相关

        private async void ListAvailablePorts()//检查可用串口
        {
            try
            {
                string aqs = SerialDevice.GetDeviceSelector();
                var dis = await DeviceInformation.FindAllAsync(aqs);
                for (int i = 0; i < dis.Count; i++)
                {
                    listOfDevices.Add(dis[i]);
                }
                DeviceListSource.Source = listOfDevices;
                ConnectDevices.SelectedIndex = -1;
            }
            catch (Exception)
            { }
        }

        private async void Listen()
        {
            dataReaderObject = new DataReader(serialPort.InputStream);
            while (true)
            {
                try
                {
                    await ReadAsync(ReadCancellationTokenSource.Token);
                }
                catch (Exception) { }
            }
        }//监听服务

        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;
            uint ReadBufferLength = 30;
            cancellationToken.ThrowIfCancellationRequested();
            dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;
            using (var childCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(childCancellationTokenSource.Token);
                UInt32 bytesRead = await loadAsyncTask;
                if (bytesRead > 0)
                {
                    try
                    {
                        string msg = dataReaderObject.ReadString(30);
                        string[] data = msg.Trim().Split(Convert.ToChar("\n"));
                        string[] Pack = data[1].Trim().Split(Convert.ToChar("#"));//防止脏读
                        if (Pack[0] == "IsHuman")
                            FileMethod.AddQuene(LocIp + "#" + "Zigbee" + "#" + data[1] + "#" + DateTime.Now.ToString());//压入队列
                    }
                    catch (Exception)
                    { }
                }
            }
        }//读取服务       

        private async void WriteCmd(string Cmd)//发送服务
        {
            try
            {
                if (serialPort != null)
                {
                    dataWriteObject = new DataWriter(serialPort.OutputStream);
                    await WriteAsync(Cmd);
                }
            }
            catch (Exception)
            { }
            finally
            {
                if (dataWriteObject != null)
                {
                    dataWriteObject.DetachStream();
                    dataWriteObject = null;
                }
            }
        }

        private async void WriteCmdByte(byte[] Cmd)//发送服务
        {
            try
            {
                if (serialPort != null)
                {
                    dataWriteObject = new DataWriter(serialPort.OutputStream);
                    await WriteByte(Cmd);
                }
            }
            catch (Exception)
            { }
            finally
            {
                if (dataWriteObject != null)
                {
                    dataWriteObject.DetachStream();
                    dataWriteObject = null;
                }
            }
        }

        private async Task WriteAsync(string Data)//发送子服务
        {
            Task<UInt32> storeAsyncTask;

            if (Data.Length != 0)
            {
                dataWriteObject.WriteString(Data);
                storeAsyncTask = dataWriteObject.StoreAsync().AsTask();
                UInt32 bytesWritten = await storeAsyncTask;
            }
        }

        private async Task WriteByte(byte[] Data)//发送子服务
        {
            Task<UInt32> storeAsyncTask;

            if (Data.Length != 0)
            {
                dataWriteObject.WriteBytes(Data);
                storeAsyncTask = dataWriteObject.StoreAsync().AsTask();
                UInt32 bytesWritten = await storeAsyncTask;
            }
        }

        private void CancelReadTask()
        {
            if (ReadCancellationTokenSource != null)
            {
                if (!ReadCancellationTokenSource.IsCancellationRequested)
                {
                    ReadCancellationTokenSource.Cancel();
                }
            }
        }//释放串口

        private void CloseDevice()
        {
            if (serialPort != null)
            {
                serialPort.Dispose();
            }
            serialPort = null;
            listOfDevices.Clear();
        }//关闭设备

        private async void BtnAddZigBee_Click(object sender, RoutedEventArgs e)
        {
            #region 配置串口
            var selection = ConnectDevices.SelectedItems;
            if (selection.Count <= 0)
            {
                return;
            }

            DeviceInformation entry = (DeviceInformation)selection[0];

            try
            {
                serialPort = await SerialDevice.FromIdAsync(entry.Id);
                if (serialPort == null) return;
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.BaudRate = 9600;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;
                serialPort.Handshake = SerialHandshake.None;
                // Create cancellation token object to close I/O operations when closing the device
                ReadCancellationTokenSource = new CancellationTokenSource();
                Listen();
            }
            catch (Exception)
            { }
            #endregion

            DeviceModel Device = new DeviceModel
            {
                NetPort = await DevicesMethod.GetPortNum() + 1,
                Type = "照明系统",
                IPAddress = "Zigbee设备"
            };
            ViewDevModel DevObj = ModelConverter.DataToView(Device);
            Obj.Add(Device);
            DevicesMethod.UpdateDevices(Obj);
            gridView.Items.Add(DevObj);
            CreateLine(Device.NetPort);//绑定新通道
            btnAddZigBee.IsEnabled = false;
        }

        #endregion

        #region 小娜相关

        private async void SaySomthing(string myDevice, string State, int speechCharacterVoice = 0)
        {
            if (myDevice == "hiActivationCMD")
                PlayVoice($"Hi Jack What can i do for you");
            else
                PlayVoice($"OK Jack {myDevice}  {State}", speechCharacterVoice);
            Debug.WriteLine($"OK -> ===== {myDevice} --- {State} =======");
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                lblContana.Text = $"OK -> ===== {myDevice} --- {State} =======";
            });
        }//子进程

        private async void PlayVoice(string tTS, int voice = 0)
        {
            //await Window.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => {
            //=====================
            // The media object for controlling and playing audio.           
            // The object for controlling the speech synthesis engine (voice).          
            SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();
            //speechSynthesizer.Voice = SpeechSynthesizer.DefaultVoice;
            speechSynthesizer.Voice = SpeechSynthesizer.AllVoices[voice];//0,4,8,12

            // Generate the audio stream from plain text.
            SpeechSynthesisStream spokenStream = await speechSynthesizer.SynthesizeTextToStreamAsync(tTS);

            await mediaElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                mediaElement.SetSource(spokenStream, spokenStream.ContentType);
                mediaElement.Play();
            }));

        }//合成声音

        public async Task<string> BegiRecongnize()
        {
            string Result = "";
            try
            {
                using (SpeechRecognizer recognizer = new SpeechRecognizer())
                {
                    SpeechRecognitionCompilationResult compilationResult = await recognizer.CompileConstraintsAsync();
                    if (compilationResult.Status == SpeechRecognitionResultStatus.Success)
                    {

                        recognizer.UIOptions.IsReadBackEnabled = false;
                        recognizer.UIOptions.ShowConfirmation = false;
                        recognizer.UIOptions.AudiblePrompt = "我在听,请说...";
                        SpeechRecognitionResult recognitionResult = await recognizer.RecognizeWithUIAsync();
                        //SpeechRecognitionResult recognitionResult = await recognizer.RecognizeAsync();
                        if (recognitionResult.Status == SpeechRecognitionResultStatus.Success)
                        {
                            Result = recognitionResult.Text;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Result = ex.Message;
            }
            return Result;
        }//前台识别声音

        public async Task<string> BackGroundRec()
        {
            string Result = "";
            try
            {
                using (SpeechRecognizer recognizer = new SpeechRecognizer())
                {
                    SpeechRecognitionCompilationResult compilationResult = await recognizer.CompileConstraintsAsync();
                    if (compilationResult.Status == SpeechRecognitionResultStatus.Success)
                    {

                        recognizer.UIOptions.IsReadBackEnabled = false;
                        recognizer.UIOptions.ShowConfirmation = false;
                        recognizer.UIOptions.AudiblePrompt = "我在听,请说...";
                        //SpeechRecognitionResult recognitionResult = await recognizer.RecognizeWithUIAsync();
                        SpeechRecognitionResult recognitionResult = await recognizer.RecognizeAsync();
                        if (recognitionResult.Status == SpeechRecognitionResultStatus.Success)
                        {
                            Result = recognitionResult.Text;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Result = ex.Message;
            }
            return Result;
        }//后台常驻声音

        private async void SoundListen()
        {
            try
            {
                while (true)
                {
                    string Str = await BackGroundRec();
                    this.Invoke(() =>
                    {
                        lblContana.Text = Str;
                    });
                    switch (Str)
                    {
                        case "你好小娜": PlayVoice("What Can I do for you"); await SoundLink(); break;
                        default:break;
                    }
                }
            }
            catch (Exception) { }
        }//监听服务

        private async Task<bool> SoundLink()
        {
            string Str = await BegiRecongnize();
            this.Invoke(() =>
            {
                lblContana.Text = Str;
            });
            switch (Str)
            {
                case "切换第一个灯":
                    WriteCmdByte(new byte[] { 0x01 });
                    PlayVoice("Yes Sir,light one Switch"); break;
                case "切换第二个灯":
                    WriteCmdByte(new byte[] { 0x02 });
                    PlayVoice("Yes Sir,light Two Switch"); break;
                default: PlayVoice("hail hydra"); break;
            }
            return true;
        }//识别服务

        private async void Test_Click(object sender, RoutedEventArgs e)
        {
            await SoundLink();
        }

        #endregion

        private async void  BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog About_dialog = new AboutDialog();
            ContentDialogResult Reslut = await About_dialog.ShowAsync();
            if (Reslut == ContentDialogResult.Primary)
            {
                
            }
        }//关于作者

        private async void Image_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)//彩蛋
        {
            ContentDialog Eggdialog = new EggtDialog();
            ContentDialogResult Reslut = await Eggdialog.ShowAsync();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }//退出按钮
    }
}
