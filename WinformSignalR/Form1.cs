using Microsoft.AspNet.SignalR.Client;
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
    public partial class Form1 : Form
    {
        //Connection to a SignalR server
        HubConnection _signalRConnection;

        //Proxy object for a hub hosted on the SignalR server
        IHubProxy _hubProxy;

        public Form1()
        {
            InitializeComponent();
        }

        private async Task ConnectAsync()
        {
            // https://stackoverflow.com/questions/15516014/is-it-good-idea-to-use-control-checkforillegalcrossthreadcalls-false
            ListBox.CheckForIllegalCrossThreadCalls = false;

            // SignalR server url and hub name. Alt url https://signalrgabs.azurewebsites.net
            string host = "http://signalrfree.bernardgabon.com"; 
            string hubName = "MessageHub";

            _signalRConnection = new HubConnection(host);
            _signalRConnection.StateChanged += HubConnection_StateChanged;

            _hubProxy = _signalRConnection.CreateHubProxy(hubName);

            // Register to the "broadcastMessage" callback method of the hub
            // This method is invoked by the hub
            _hubProxy.On<string, string>("broadcastMessage", (name, message) => OnMessageReceived(name, message) );

            btnConnect.Enabled = false;

            try
            {
                // Connect to the server
                await _signalRConnection.Start();

                // Send user name for this client, so we won't need to send it with every message
                await _hubProxy.Invoke("Send", txtUsername.Text, " Joining the chat... ");
            }
            catch
            {
                btnConnect.Enabled = true;
            }
        }

        private void HubConnection_StateChanged(StateChange scObj)
        {
            if (scObj.NewState == Microsoft.AspNet.SignalR.Client.ConnectionState.Connected)
            {
                MessageBox.Show("Connected.");
                txtUsername.Enabled = false;
                this.AcceptButton = btnSend;
                btnConnect.Enabled = false;
                btnSend.Enabled = true;
                txtMessage.Focus();          
            }
            else if (scObj.NewState == Microsoft.AspNet.SignalR.Client.ConnectionState.Disconnected)
            {
                this.Text += " [Disconnected]";
                btnSend.Enabled = false;
                btnConnect.Enabled = true;
            }
        }

        private void OnMessageReceived(string from, string msg)
        {
            listBox1.Items.Add($"{from}: {msg}");
        }

        // Make this button event "async"
        private async void btnConnect_Click(object sender, EventArgs e)
        {
            await ConnectAsync();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            // Call the "Send" method on the hub (on the server) with the given parameters
            _hubProxy.Invoke("Send", txtUsername.Text, txtMessage.Text).Wait();
            txtMessage.Text = string.Empty;
        }
    }
}
