using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;   
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ScreenProjection_Client
{
    
    public partial class Form1 : MetroFramework.Forms.MetroForm
    {
        private TcpClient server = null;
        private NetworkStream mainStream = null;

        private int portRequestConnect = 10063;
        private int portSendDesktop = 10064;
        String myip = "";
        String ipServer = "";

        private Thread requestProcessThread;
        private Thread sendDesktopThread;

        /*
         * nb: set ip requstScreenProjector and ip TcpClient using ipServer
         */

        public Form1()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            requestProcessThread = new Thread(requestProcess);
            requestProcessThread.Start();
            button1.Enabled = false;
        }

        private void requestProcess()
        {
            ipServer = getIPServer();

            if (requestScreenProjector())
            {
                
                server = new TcpClient();
                while (!server.Connected)
                {
                    try
                    {
                        //##
                        server.Connect(ipServer, portSendDesktop);
                        sendDesktopThread = new Thread(SendImage);
                        sendDesktopThread.Start();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            else
            {
                button1.BeginInvoke(new MethodInvoker(() =>
                {
                    button1.Enabled = true;
                }));
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            myip = getIP();
        }
        private String getIP()
        {
            string hostname = string.Empty;
            hostname = Dns.GetHostName();
            IPHostEntry iPHostEntry = Dns.GetHostEntry(hostname);
            IPAddress[] addr = iPHostEntry.AddressList;
            for (int i = 0; i < addr.Length; i++)
            {
                if (addr[i].ToString().Contains("10."))
                {
                    return addr[i].ToString();
                }
            }
            return "";
        }

        private String getIPServer()
        {
            string[] tempip = myip.Split('.');
            return tempip[0] + "." + tempip[1] + "." + tempip[2] + "." + "141";
        }

        private bool requestScreenProjector()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //String ipServer = getIPServer();
            //Set IPEndPoint using ipServer
            //##
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(ipServer), portRequestConnect);

            String msg = myip;
            byte[] data = Encoding.ASCII.GetBytes(msg);
            try
            {
                socket.Connect(iPEndPoint);
                socket.SendTimeout = 500;
                socket.Send(data);

                byte[] messageRecv = new byte[1024];
                int byteRecv = socket.Receive(messageRecv);
                string recvMsg = Encoding.ASCII.GetString(messageRecv, 0, byteRecv);

                socket.Close();
                if (recvMsg.Equals("yes")) return true;
                else return false;
            }
            catch (Exception)
            {
                socket.Close();
                return false;
            }

        }        

        private void SendImage()
        {
            while (server.Connected)
            {
                SendDesktopImage();
            }
            server = null;
        }

        private static Image GrabDesktop()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            Graphics grapich = Graphics.FromImage(screenshot);
            grapich.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            return screenshot;
        }
        private void SendDesktopImage()
        {
            BinaryFormatter binformater = new BinaryFormatter();
            Image img = GrabDesktop();
            try
            {
                mainStream = server.GetStream();
                binformater.Serialize(mainStream, img);
            }
            catch (Exception)
            {
                mainStream.Close();
                mainStream = null;
                server.Close();
                button1.BeginInvoke(new MethodInvoker(() =>
                {
                    button1.Enabled = true;
                }));
            }
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
        }
    }
}
