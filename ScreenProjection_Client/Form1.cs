using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Shell;

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
            //MessageBox.Show("Myip: " + getIPServer());
            
            ipServer = getIPServer();

            if (requestScreenProjector())
            {
                
                server = new TcpClient();
                while (!server.Connected)
                {
                    try
                    {
                        //##
                        server.Connect(myip, portSendDesktop);
                        timer1.Start();
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
            //set IPEndPoint using ipServer
            //##
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(myip), portRequestConnect);

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

                MessageBox.Show(recvMsg);
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
                Console.WriteLine("send");
            }
            catch (Exception)
            {
                Console.WriteLine("serialize");
                timer1.Stop();
                mainStream = null;
                server.Close();
            }
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            SendDesktopImage();
        }
    }
}
