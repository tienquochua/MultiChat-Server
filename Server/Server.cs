using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public partial class Server : Form
    {
        IPEndPoint IP;
        Socket server;
        string ipAddress;
        List<Socket> ClientList;


        public Server()
        {
            InitializeComponent();
            Connect();
            CheckForIllegalCrossThreadCalls = false;
        }

        private void Server_Load(object sender, EventArgs e)
        {
            this.NewFileRecieved += new FileRecievedEventHandler(Form1_NewFileRecieved);
        }
        void Connect()
        {
            ClientList = new List<Socket>();
            IP = new IPEndPoint(IPAddress.Any, 9050);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(IP);

            Thread Listen = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        server.Listen(100);
                        Socket client = server.Accept();
                        ClientList.Add(client);
                        Thread recieve = new Thread(recieveData);
                        recieve.IsBackground = true;
                        recieve.Start(client);
                    }
                }
                catch
                {
                    IP = new IPEndPoint(IPAddress.Any, 9050);
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                }
            });
            Listen.IsBackground = true;
            Listen.Start();
        }
        public string GetipAddress()
        {
            IPHostEntry Host = default(IPHostEntry);
            string Hostname = null;
            Hostname = System.Environment.MachineName;
            Host = Dns.GetHostEntry(Hostname);
            foreach (IPAddress IP in Host.AddressList)
            {
                if (IP.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddress = Convert.ToString(IP);
                }
            }
            return ipAddress;
        }
        public void recieveData(object obj)
        {
            Socket client = obj as Socket;
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    int rec = client.Receive(data, SocketFlags.None);
                    string s = Encoding.UTF8.GetString(data, 0, rec);
                    string[] tmp = s.Split('|');
                    switch (tmp[0])
                    {
                        case "Connect":
                            ListViewItem lv = new ListViewItem(GetipAddress());
                            lv.SubItems.Add(tmp[1]);
                            lv.SubItems.Add(tmp[2]);
                            lvClient.Items.Add(lv);
                            txtReceive.Text += "<< " + tmp[1] + " joined the room >>\n";
                            string user = string.Empty;
                            for (int i = 0; i < lvClient.Items.Count; i++)
                            {
                                user += lvClient.Items[i].SubItems[1].Text + "|";
                            }
                            textBox1.Text = "Comming|" + txtReceive.Text;
                            textBox2.Text = "Refresh|" + user.TrimEnd('|');
                            foreach (Socket item in ClientList)
                             {
                                item.Send(Encoding.UTF8.GetBytes(textBox1.Text));
                                item.Send(Encoding.UTF8.GetBytes(textBox2.Text));
                             }
                            break;
                        case "Message":

                            textBox1.Text = "Users|" + tmp[1] + ":" + tmp[2];
                            foreach (Socket item in ClientList)
                            {
                                if (item != null && item != client)
                                    item.Send(Encoding.UTF8.GetBytes(textBox1.Text));

                            }
                            txtReceive.Text += tmp[1] + ":" + tmp[2] + "\n";
                            break;
                        case "Remove":
                            txtReceive.Text += "<< " + tmp[1] + " has left the room >>\n";
                            for (int i = 0;i<lvClient.Items.Count;i++)
                            {
                                if (tmp[1] == lvClient.Items[i].SubItems[1].Text)
                                    lvClient.Items.RemoveAt(i);
                            }
                            string user1 = string.Empty;
                            for (int i = 0; i < lvClient.Items.Count; i++)
                            {
                                user1 += lvClient.Items[i].SubItems[1].Text + "|";
                            }
                            textBox2.Text = "Refresh|" + user1.TrimEnd('|');
                            
                            foreach (Socket item in ClientList)
                            {
                                item.Send(Encoding.UTF8.GetBytes(textBox2.Text));
                                item.Send(Encoding.UTF8.GetBytes("Left|" + txtReceive.Text));
                            }
                            break;
                        case "File":
                            byte[] bytes = Convert.FromBase64String(tmp[1]);
                            File.WriteAllBytes("C:\\Users\\ASUS\\Desktop\\MultiChat\\MultiChat\\Server\\FileReceive\\" + tmp[2], bytes);
                            txtReceive.Text += "Received File: " + tmp[2] + "\n";
                            break;
                    }
                }
            }
            catch
            {
                ClientList.Remove(client);
                client.Close();
            }
        }
              
        private void btnSend_Click(object sender, EventArgs e)
        {

            if (txtInput.Text != string.Empty)
            {
                byte[] data = new byte[1024];
                string export = "Server|" + txtInput.Text;
                data = Encoding.UTF8.GetBytes(export);
                foreach (Socket item in ClientList)
                {
                    item.Send(data);
                }
            }
            txtReceive.Text += "Server:" + txtInput.Text + "\n";
            txtInput.Clear();
        }

        //Xu ly nhan file tu client
        IPAddress localAddr = IPAddress.Parse("127.0.0.1");
        public delegate void FileRecievedEventHandler(object source, string fileName);
        public event FileRecievedEventHandler NewFileRecieved;
        private void Form1_NewFileRecieved(object sender, string fileName)
        {
            this.BeginInvoke(new Action(delegate ()
            {
                MessageBox.Show("File mới nhận: \n" + fileName);
                System.Diagnostics.Process.Start("explorer", @"D:\");
            }));
        }
        public void HandleIncomingFile(IPAddress address, int port)
        {
            try
            {
                TcpListener tcpListener = new TcpListener(localAddr, port);
                tcpListener.Start();
                while (true)
                {
                    Socket handlerSocket = tcpListener.AcceptSocket();
                    if (handlerSocket.Connected)
                    {
                        string fileName = string.Empty;
                        NetworkStream networkStream = new NetworkStream(handlerSocket);
                        int thisRead = 0;
                        int blockSize = 1024;
                        Byte[] dataByte = new Byte[blockSize];
                        lock (this)
                        {
                            string folderPath = @"D:\";
                            int receivedBytesLen = handlerSocket.Receive(dataByte);
                            int fileNameLen = BitConverter.ToInt32(dataByte, 0);
                            fileName = Encoding.ASCII.GetString(dataByte, 4, fileNameLen);
                            Stream fileStream = File.OpenWrite(folderPath + fileName);
                            fileStream.Write(dataByte, 4 + fileNameLen, (1024 - (4 + fileNameLen)));
                            while (true)
                            {
                                thisRead = networkStream.Read(dataByte, 0, blockSize);
                                fileStream.Write(dataByte, 0, thisRead);
                                if (thisRead == 0)
                                    break;
                            }
                            fileStream.Close();
                        }
                        if (NewFileRecieved != null)
                        {
                            NewFileRecieved(this, fileName);
                        }
                        handlerSocket = null;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("" + e);
            }
        }

        private void btnNhanFile_Click(object sender, EventArgs e)
        {
            int port = 9999;
            Task.Factory.StartNew(() => HandleIncomingFile(localAddr, port));
            MessageBox.Show("Đang nghe ở cổng >> " + port);
        }

        private void txtReceive_TextChanged(object sender, EventArgs e)
        {
            txtReceive.SelectionStart = txtReceive.Text.Length;
            txtReceive.ScrollToCaret();
        }
    }
}
