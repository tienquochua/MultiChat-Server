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

namespace Client
{
    public partial class Client : Form
    {
        Socket client;
        IPEndPoint IP;
        
        private static string shortFileName = "";
        private static string fileName = "";
        public Client()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        
        void Connect()
        {
            IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9050);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                client.Connect(IP);
                byte[] data = new byte[1024];
                string export = "Connect|" + txtName.Text + "|" + "Connected";
                data = Encoding.UTF8.GetBytes(export);
                client.Send(data, SocketFlags.None);
            }
            catch
            {
                MessageBox.Show("Không thể kết nối tới server", "Lỗi!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Thread listen = new Thread(Receive);
            listen.IsBackground = true;
            listen.Start();
        }
        void Receive()
        {
            
            try
            {

                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    int recv = client.Receive(data, SocketFlags.None);
                    string s = Encoding.UTF8.GetString(data, 0, recv);
                    string[] tmp = s.Split('|');
                    switch(tmp[0])
                    {
                        case "Users":
                            txtReceive.Text += tmp[1] + "\n";
                            break;
                        case "Server":
                            txtReceive.Text += "Server:" + tmp[1] + "\n";
                            break;
                        case"Comming":
                            txtReceive.Text = tmp[1];
                            break;
                        case "Left":
                            txtReceive.Text = tmp[1];
                            break;
                        case "Refresh":
                            userList.Items.Clear();
                            for (int i = 1; i < tmp.Length; i++)
                            {
                                userList.Items.Add(tmp[i]);
                            }
                            break;
                    }
                    
                }
            }
            catch
            {
                Close();
            }
        }
        void close()
        {
            client.Close();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (txtInput.Text != string.Empty)
                {
                    byte[] data = new byte[1024];
                    string export = "Message|" + txtName.Text + "|" + txtInput.Text;
                    data = Encoding.UTF8.GetBytes(export);
                    client.Send(data, SocketFlags.None);
                    txtReceive.Text += txtName.Text + ":" + txtInput.Text + "\n";
                    txtInput.Clear();
                }
            }
            catch
            {
               
            }
            
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if(txtName.Text != string.Empty)
            {
                Connect();
                btnConnect.Enabled = false;
                txtName.Enabled = false;
            }
            
        }

        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            byte[] data = new byte[1024];
            string export = "Remove|" + txtName.Text;
            data = Encoding.UTF8.GetBytes(export);
            client.Send(data, SocketFlags.None);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        //send file

        private void btnSendFile_Click(object sender, EventArgs e)
        {
            string ipAddress = "127.0.0.1";
            int port = 9050;
            string fileName = txtFile.Text;
            Task.Factory.StartNew(() => SendFile(ipAddress, port, fileName, shortFileName));
            MessageBox.Show("File Đã Gửi");
            txtFile.Clear();
        }
        string fileString;
        string fname;
        void ConvertFileToBase64String(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            fileString = Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks);
            string filePathReplace = path;
            filePathReplace = filePathReplace.Replace("\\", "/");
            fname = filePathReplace.Substring(filePathReplace.LastIndexOf('/') + 1);
        }

        private void btnChonFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Chon Thu Muc";
            dlg.ShowDialog();
            txtFile.Text = dlg.FileName;
            fileName = dlg.FileName;
            shortFileName = dlg.SafeFileName;
        }
        public void SendFile(string remoteHostIP, int remoteHostPort,
          string longFileName, string shortFileName)
        {
            try
            {
                if (!string.IsNullOrEmpty(remoteHostIP))
                {
                    
                    string filePath = txtFile.Text;
                    ConvertFileToBase64String(filePath);
                    txtReceive.Text += "Sending " + filePath + "\n";
                    client.Send(Encoding.UTF8.GetBytes("File|" + fileString + "|" + fname), 0);
                    txtReceive.Text += "COMPLETE!!!" + "\n";
                    
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("" + e);
            }
        }

        private void txtReceive_TextChanged(object sender, EventArgs e)
        {
            txtReceive.SelectionStart = txtReceive.Text.Length;
            txtReceive.ScrollToCaret();
        }
    }

        
}
