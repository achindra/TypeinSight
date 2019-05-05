using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using System.Threading;
using Newtonsoft.Json;
using System.Text;
using TypeInSight;
using System.Collections;

namespace TypeinSight
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KeyboardListener kbListner = new KeyboardListener();
        public static NotifyIcon notifyIcon = new NotifyIcon();

        static string deviceId = Environment.UserName.ToUpper() + "__" + Environment.MachineName.ToUpper();
        static string deviceKey;
        static DeviceClient deviceClient;
        static RegistryManager registryManager;
        static string iotHubUri = "TypeInSightHub.azure-devices.net";
        static string connectionString = "HostName=TypeInSightHub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=xLDhhyx695u0yEtvHhMIxbOfEJVWcdVzr+xqOcL68CM=";

        private uint[] KeyDownTime = new uint[256];
        private static DateTime baseDate = new DateTime(1970, 1, 1);
        private static double epoc = (DateTime.UtcNow - baseDate).TotalMilliseconds;
        private static ulong BootTime = (ulong)(epoc - Environment.TickCount);

        private ulong dataCollected = 0;
        private static LoggingUtility logger = new LoggingUtility();

        private nGrams _1Grams = new nGrams(1);
        private nGrams _3Grams = new nGrams(3);
        private nGrams _5Grams = new nGrams(5);
        private TypedWord _typedWord = new TypedWord();

        private static Queue msgQueue = new Queue();
        private static Thread msgThread = new Thread(ThreadProc);
        private static ManualResetEventSlim dataQueued = new ManualResetEventSlim(false);
        private static ManualResetEventSlim procDead = new ManualResetEventSlim(false);


        public MainWindow()
        {
            InitializeComponent();

            using (Bitmap bmp = Resource.favicon.ToBitmap())
            {
                var stream = new MemoryStream();
                bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                this.Icon = BitmapFrame.Create(stream);
            }

            lblHeader.Content = "Welcome " + Environment.UserName.ToUpper() + " on " + Environment.MachineName.ToUpper();

            registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            AddDeviceAsync();
            deviceClient = DeviceClient.Create(iotHubUri,
                                               new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey),
                                               Microsoft.Azure.Devices.Client.TransportType.Mqtt_WebSocket_Only);
            deviceClient.ProductInfo = "TypeInSight";

            kbListner.Hook();
            kbListner.KeyDown += KbListner_KeyDown;
            kbListner.KeyUp += KbListner_KeyUp;

            notifyIcon.Icon = Resource.favicon;
            notifyIcon.BalloonTipTitle = "TypeInSight";
            notifyIcon.BalloonTipText = "Watching your keyboard";
            notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon.Text = "TypeInSight";
            notifyIcon.Visible = true;
            notifyIcon.Click += NotifyIcon_Click;

            this.StateChanged += MainWindow_StateChanged;
            this.Hide();
            msgThread.Start();
            logger.WriteToLog("Watcher started for user " + Environment.UserName);
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            if (IsVisible)
            {
                Activate();
            }
            else
            {
                Show();
            }
        }

        private void KbListner_KeyDown(object sender, KeyboardListener.KBDLLHOOKSTRUCT e)
        {
            KeyDownTime[e.vkCode] = e.time;
        }

        private void KbListner_KeyUp(object sender, KeyboardListener.KBDLLHOOKSTRUCT e)
        {
            uint _keyStart = KeyDownTime[e.vkCode];

            //skip Caps, Shifts, Ctrls, Window, Alts, RClicks
            char c = Convert.ToChar(e.vkCode);
            if ((c >= 160 && c <= 165) || (c >= 91 && c <= 93) || c == 9 || c == 20)
                return;

            notifyIcon.BalloonTipText = "Trapped" + ++dataCollected + "Key strokes";
            lblCount.Content = dataCollected;

            TypedData _1GData = _1Grams.GetTypedData(e.vkCode, e.time - _keyStart);
            if (null != _1GData && null != _1GData.strData)
            {
                msgQueue.Enqueue(_1GData);
                dataQueued.Set();
            }

            TypedData _3GData = _3Grams.GetTypedData(e.vkCode, e.time - _keyStart);
            if (null != _3GData && null != _3GData.strData)
            {
                msgQueue.Enqueue(_3GData);
                dataQueued.Set();
            }

            TypedData _5GData = _5Grams.GetTypedData(e.vkCode, e.time - _keyStart);
            if (null != _5GData && null != _5GData.strData)
            {
                msgQueue.Enqueue(_5GData);
                dataQueued.Set();
            }

            TypedData _TyData = _typedWord.GetTypedData(e.vkCode, e.time - _keyStart);
            if (null != _TyData && null != _TyData.strData)
            {
                msgQueue.Enqueue(_TyData);
                dataQueued.Set();
            }
        }

        private static void AddDeviceAsync()
        {
            Device device;
            try
            {
                device = registryManager.AddDeviceAsync(new Device(deviceId)).GetAwaiter().GetResult();
            }
            catch (DeviceAlreadyExistsException)
            {
                device = registryManager.GetDeviceAsync(deviceId).GetAwaiter().GetResult();
            }
            logger.WriteToLog("Device key: " + device.Authentication.SymmetricKey.PrimaryKey);
            deviceKey = device.Authentication.SymmetricKey.PrimaryKey;
        }

        private static void ThreadProc()
        {
            while (!procDead.IsSet)
            {
                dataQueued.Wait();
                while (msgQueue.Count != 0 && !procDead.IsSet)
                {
                    SendDeviceToCloudMessagesAsync(JsonConvert.SerializeObject(msgQueue.Dequeue()));
                }
                if (msgQueue.Count == 0)
                    dataQueued.Reset();
            }
        }

        private static async void SendDeviceToCloudMessagesAsync(string messageString)
        {
            var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));
            try
            {
                await deviceClient.SendEventAsync(message);
            }
            catch (AggregateException ClosedChannelException)
            {
                logger.WriteToLog("Handling " + ClosedChannelException.Message);
                await deviceClient.CloseAsync();
                await deviceClient.OpenAsync();
                await deviceClient.SendEventAsync(message);
            }
            logger.WriteToLog("Sending message: " + messageString);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            kbListner.KeyDown -= KbListner_KeyDown;
            kbListner.UnHook();
            deviceClient.CloseAsync().GetAwaiter().GetResult();
            deviceClient.Dispose();
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            msgQueue.Clear();
            procDead.Set();
            //msgThread.Join();
        }

        private void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (btnPlayPause.Content.ToString() == "Pause")
            {
                notifyIcon.BalloonTipIcon = ToolTipIcon.Warning;
                notifyIcon.BalloonTipText = "Watching keyboard Paused";
                btnPlayPause.Content = "Start";
                kbListner.KeyDown -= KbListner_KeyDown;
                kbListner.UnHook();
            }
            else
            {
                notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                btnPlayPause.Content = "Pause";
                kbListner.KeyDown += KbListner_KeyDown;
                kbListner.Hook();
            }
        }
    }

}
