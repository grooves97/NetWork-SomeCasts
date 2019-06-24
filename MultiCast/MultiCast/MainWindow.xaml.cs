using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace MultiCast
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string _IpMultiCast = "224.1.1.1";
        public static string _MyIp = "192.168.56.1";
        public static int _port = 12345;
        public static int _mcTTL = 2;

        public MainWindow()
        {
            InitializeComponent();
            // 1 - 0.0.0.0 = IpAddressAny
            Param p1 = new Param(IPAddress.Any, 12345);
            p1.IpMulticast = IPAddress.Parse(_IpMultiCast);
            p1.TextBox = firstTextBox;
            Thread th1 = new Thread(ThreadProcReciv);
            th1.IsBackground = true;
            th1.Start(p1);

            { // 2 - 192.168.1.67 = 
                Param p2 = new Param(IPAddress.Parse(_MyIp), 12345);
                p2.IpMulticast = IPAddress.Parse(_IpMultiCast);
                p2.TextBox = secondTextBox;
                Thread th2 = new Thread(ThreadProcReciv);
                th2.IsBackground = true;
                th2.Start(p2);
            }
        }

        delegate void AppendTextOut(TextBox txtBox, string str);
        void AppendTextProc(TextBox txtBox, string str)
        {
            txtBox.AppendText(str + "\n");
        }

        public class Param
        {
            public IPAddress Ip { get; set; }
            public IPAddress IpMulticast { get; set; }
            public int Ports { get; set; }
            public bool IsStop { get; set; }
            public TextBox TextBox { get; set; }
            public Param(IPAddress ipAddr, int Port)
            {
                IpMulticast = IPAddress.Parse("224.5.5.5");
                Ip = ipAddr;
                Ports = Port;
                IsStop = false;
            }
        }

        void ThreadProcSend(object obj)
        {
            Socket sockMulticast = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sockMulticast.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, _mcTTL);

            IPAddress ipDest = IPAddress.Parse(_IpMultiCast);
            //IPAddress ipDest = IPAddress.Parse("224.5.5.5");
            //int port = 12345;
            sockMulticast.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ipDest));
            IPEndPoint ipep = new IPEndPoint(ipDest, _port);
            sockMulticast.Connect(ipep);

            string message = "Hello Network!";
            for (int i = 1; i <= 5; i++)
            {
                string temp = i.ToString() + ") " + message;
                Dispatcher.Invoke(new AppendTextOut(AppendTextProc), anotherTextBox, temp);
                sockMulticast.Send(Encoding.Default.GetBytes(temp));
                Thread.Sleep(500);
            }
            Dispatcher.Invoke(new AppendTextOut(AppendTextProc), firstTextBox, "");
            sockMulticast.Close();
        }

        void ThreadProcReciv(object obj)
        {
            Param p = obj as Param;
            Dispatcher.Invoke(new AppendTextOut(AppendTextProc), p.TextBox, "Start TreadProcReciv()");
            Dispatcher.Invoke(new AppendTextOut(AppendTextProc), p.TextBox, "EP: " + p.Ip.ToString() + ":" + p.Ports.ToString());
            Dispatcher.Invoke(new AppendTextOut(AppendTextProc), p.TextBox, "Multicast IP: " + p.IpMulticast.ToString());

            Socket sockRec = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sockRec.Bind(new IPEndPoint(p.Ip, p.Ports));
            sockRec.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
              new MulticastOption(p.IpMulticast, p.Ip));

            byte[] buf = new byte[4 * 1024];
            while (!p.IsStop)
            {
                int size = sockRec.Receive(buf);
                Dispatcher.Invoke(new AppendTextOut(AppendTextProc), p.TextBox, Encoding.Default.GetString(buf, 0, size));
            }
            sockRec.Close();
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            Param pp = new Param(IPAddress.Parse("192.168.1.201"), 12345);
            ThreadPool.QueueUserWorkItem(ThreadProcSend);
        }

        private void ButtonCloseClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
