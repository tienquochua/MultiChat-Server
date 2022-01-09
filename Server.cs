using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.IO;
using System.Threading.Tasks;
namespace FileSharingServer
{ 

public class Class1
{
    public delegate void FileRecievedEventHandler(object source, string fileName);
    public event FileRecievedEventHandler NewFileRecieved;
    public Class1()
    {
        InitializeComponent();
    }
    private void Form1_Load(object sender, EventArgs e)
    {
        this.NewFileRecieved += new FileRecievedEventHandler(Form1_NewFileRecieved);
    }
    private void Form1_NewFileRecieved(object sender, string fileName)
    {
        this.BeginInvoke(new Action(delegate ()
        {
            MessageBox.Show("New File Received\n" + fileName);
            System.Diagnostics.Process.Start("explorer", @"c:\");
        }));
    }
    private void btnListen_Click(object sender, EventArgs e)
    {
        int port = int.Parse(txtHost.Text);
        Task.Factory.StartNew(() => HandleIncomingFile(port));
        MessageBox.Show("Listening on port" + port);
    }
    public void HandleIncomingFile(int port)
    {
        try
        {
            TcpListener tcpListener = new TcpListener(port);
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
                        string folderPath = @"c:\";
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
        catch { }
    }
}
}