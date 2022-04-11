using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public partial class Server : Form
    {
        public Server()
        {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;

            Connect();
        }

        private void Server_FormClosed(object sender, FormClosedEventArgs e)
        {
            Close();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            foreach (Socket item in clientList)
            {
                Send(item);
            }
            AddMessage(txtMessenger.Text);
            txtMessenger.Clear();
        }

        IPEndPoint IP;
        Socket server;
        List<Socket> clientList;

        //Kết nối tới Server
        void Connect()
        {
            clientList = new List<Socket>();
            IP = new IPEndPoint(IPAddress.Any, 2021);
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
                        clientList.Add(client);

                        Thread receive = new Thread(Receive);
                        receive.IsBackground = true;
                        receive.Start(client);
                    }
                }
                catch
                {
                    IP = new IPEndPoint(IPAddress.Any, 2021);
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }

            });

            Listen.IsBackground = true;
            Listen.Start();
        }

        //Đóng kết nối hiện thời 
        void Close()
        {
            server.Close();
        }

        //Gửi tin
        void Send(Socket client)
        {
            if (client != null && txtMessenger.Text != string.Empty)
            {
                client.Send(Serialize(txtMessenger.Text));
            }
        }

        //Nhận tin
        int id = -1;

        void Receive(object obj)
        {

            Socket client = obj as Socket;
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    client.Receive(data);
                    obj = Deserialize(data);

                    if (obj.GetType().ToString() == "System.String")
                    {
                        DateTime dt = DateTime.Now;
                        lVChat.Items.Add((String)obj + " " + "(" + dt.Hour + ":" + dt.Minute + ":" + dt.Second + ")");
                    }
                    else
                    if (obj.GetType().ToString() == "System.Drawing.Bitmap")
                    {
                        id++;
                        Image image = (Image)Deserialize(data);
                        imageL.Images.Add(id + "", image);
                        ListViewItem item = new ListViewItem();
                        item.ImageKey = id + "";
                        lVChat.Items.Add(item);
                    }
                    lVChat.EnsureVisible(lVChat.Items.Count - 1);
                }
            }
            catch
            {
                clientList.Remove(client);
                client.Close();
            }

        }

        void addImg(string p)
        {
            id++;
            imageL.Images.Add(Image.FromFile(p));
            ListViewItem item = new ListViewItem();
            item.ImageIndex = id;
            lVChat.Items.Add(item);

        }

        private void btnImage_Click(object sender, EventArgs e)
        {
            try
            {
                String path = @"";
                OpenFileDialog open = new OpenFileDialog();
                open.ShowDialog();
                path = open.FileName;
                Image image = Image.FromFile(path);
                byte[] data = new byte[1024 * 5000];
                data = Serialize(image);
                foreach (Socket item in clientList)
                {
                    item.Send(data);
                    addImg(path);
                }
            }
            catch (Exception)
            {
            }

        }

        private void btnEmoij_Click(object sender, EventArgs e)
        {
            LVEmoij.Visible = true;
        }

        //Add message vào khung chat
        void AddMessage(string s)
        {
            DateTime dt = DateTime.Now;
            lVChat.Items.Add(new ListViewItem() { Text = s + " " + "(" + dt.Hour + ":" + dt.Minute + ":" + dt.Second + ")" });
        }


        //Phân mảnh
        byte[] Serialize(object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, obj);

            return stream.ToArray();
        }

        //Gom mảnh lại
        object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();

            return formatter.Deserialize(stream);
        }

        private void LVEmoij_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                int i = LVEmoij.SelectedIndices[0];
                if (i < 0) return;
                byte[] gui = new byte[1024 * 5000];
                gui = Serialize(imageE.Images[i]);

                id++;
                Image image = (Image)imageE.Images[i];
                imageL.Images.Add(id + "", image);
                ListViewItem it = new ListViewItem();
                it.ImageKey = id + "";
                lVChat.Items.Add(it);
                LVEmoij.Visible = false;

                foreach (Socket item in clientList)
                {
                    if (item != null)
                    {
                        item.Send(gui);
                    }
                }
                lVChat.EnsureVisible(lVChat.Items.Count - 1);
            }
            catch
            {
            }
        }

        int chonItem = -1;

        private void xóaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (chonItem == -1)
                    return;
                lVChat.Items[chonItem].Text = "Đã Xóa";
                lVChat.Items[chonItem].ImageKey = "";
            }
            catch
            {
            }
        }

        private void lVChat_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                chonItem = lVChat.SelectedIndices[0];
            }
            catch
            {
            }
        }
    }
}
