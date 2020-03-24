using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinformSignalR
{
    public partial class FormSendImage : Form
    {
        //Connection to a SignalR server
        HubConnection _signalRConnection;

        //Proxy object for a hub hosted on the SignalR server
        IHubProxy _hubProxy;

        byte[] imgDataResized;

        public FormSendImage()
        {
            InitializeComponent();
        }

        private async Task ConnectAsync()
        {
            // https://stackoverflow.com/questions/15516014/is-it-good-idea-to-use-control-checkforillegalcrossthreadcalls-false
            Form.CheckForIllegalCrossThreadCalls = false;

            // SignalR server url and hub name. Alt url https://signalrgabs.azurewebsites.net
            string host = "http://signalrfree.bernardgabon.com";
            string hubName = "MessageHub";

            _signalRConnection = new HubConnection(host);
            _signalRConnection.StateChanged += HubConnection_StateChanged;

            _hubProxy = _signalRConnection.CreateHubProxy(hubName);

            // Register to the "broadcastMessage" callback method of the hub
            // This method is invoked by the hub
            _hubProxy.On<string, string>("broadcastMessage", (channel, msgDataWithImage) => ReceiveMessage(channel, msgDataWithImage));

            try
            {
                // Connect to the server
                await _signalRConnection.Start();
            }
            catch
            {
                MessageBox.Show("Failed to connect.");                
            }
        }

        private void HubConnection_StateChanged(StateChange scObj)
        {
            if (scObj.NewState == Microsoft.AspNet.SignalR.Client.ConnectionState.Connected)
            {
                this.Text += " [CONNECTED]";
                btnBrowse.Enabled = true;
            }
            else if (scObj.NewState == Microsoft.AspNet.SignalR.Client.ConnectionState.Disconnected)
            {
                this.Text += " [Disconnected]";
            }
        }

        private void ReceiveMessage(string channel, string msgDataWithImage)
        {
            if(channel == txtChannel.Text)
            {
                ImageExchange img = JsonConvert.DeserializeObject<ImageExchange>(msgDataWithImage);
                var pic = img.ImgData.ConvertByteArrayToImage();

                imageList1.ImageSize = new Size(256, 256);
                imageList1.ColorDepth = ColorDepth.Depth32Bit;
                imageList1.Images.Add(pic);

                listView1.LargeImageList = imageList1;

                ListViewItem item = new ListViewItem();
                item.ImageIndex = imageList1.Images.Count - 1;
                item.Text = img.Name;
                listView1.Items.Add(item);
                tabControl1.SelectedIndex = 0;
            }
        }


        private async void FormSendImage_Load(object sender, EventArgs e)
        {
            await ConnectAsync();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Image File|*.jpg;*.jpeg;*.png;*gif";
            fileDialog.ShowDialog();

            // the original image
            byte[] imgDataFromFile = System.IO.File.ReadAllBytes(fileDialog.FileName);

            // the optimized image which you can save to the database
            imgDataResized = imgDataFromFile.ConvertToImageByteArray(256);

            // converts image byte array to Bitmap for use in ImageList
            Bitmap pic = imgDataResized.ConvertByteArrayToImage();

            pictureBox1.Image = pic;
            tabControl1.SelectedIndex = 1;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            var msgDataWithImage = new ImageExchange();
            msgDataWithImage.Name = Interaction.InputBox("Name of this photo");
            msgDataWithImage.ImgData = imgDataResized;
            var strMsgData = JsonConvert.SerializeObject(msgDataWithImage);

            _hubProxy.Invoke("Send", txtChannel.Text, strMsgData).Wait();            
        }
    }
}
