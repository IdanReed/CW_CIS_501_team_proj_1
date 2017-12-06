﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FP_Core;
using FP_Core.Events;

namespace FP_Team01
{
    public partial class MainMenu : Form
    {
        string Username;
        public MainMenu()
        {
            Username = Program.USERNAME;
            InitializeComponent();
            uxLabelName.Text = "Welcome, " + Username + ".";
            NetworkHandler.eObs += ReceiveErrorMessage;
            NetworkHandler.mObs += ReceiveFromServer;
            Event getAllContacts = new Event(Username, EventTypes.SendAllContacts);
            SendToServer(getAllContacts);
        }

        private void ReceiveErrorMessage(string errMessage)
        {
            MessageBox.Show(errMessage);
        }

        private void BtnAddContact_Click(object sender, EventArgs e)
        {
            string contactToAdd = uxTxtUsername.Text;
            SendContactEventData evtData = new SendContactEventData(contactToAdd);
            Event evt = new Event(evtData, EventTypes.AddContactEvent);
            SendToServer(evt);
        }

        private void BtnRemoveContact_Click(object sender, EventArgs e)
        {
            string contactToAdd = uxTxtUsername.Text;
            SendContactEventData evtData = new SendContactEventData(contactToAdd);
            Event evt = new Event(evtData, EventTypes.RemoveContactEvent);
            SendToServer(evt);
        }

        private void BtnSignOut_Click(object sender, EventArgs e)
        {
            //Send signout event to server and return to login window, where you can safely close program
            /*this.Close();
            Environment.Exit(0);*/
            Form_Closing(sender, null);
        }

        private void BtnStartChat_Click(object sender, EventArgs e)
        {
            //ClientAccount contactToChat = uxLBContacts.SelectedItem as ClientAccount;
            string contactName = uxLBContacts.SelectedItem.ToString();
            //Send friend name to server and start chat form

            //From https://stackoverflow.com/questions/5548746/c-sharp-open-a-new-form-then-close-the-current-form
            /*this.Hide();
            var chat = new ChatForm();
            chat.FormClosed += (s, args) => this.Close();
            chat.Show();*/
            JoinChatroomEventData evtData = new JoinChatroomEventData(contactName, -1);
            Event evt = new Event(evtData, EventTypes.CreateChatEvent);
            SendToServer(evt);
        }

        public void ReceiveFromServer(Event evt)
        {
            switch (evt.Type)
            {
                case EventTypes.ContactWentOnline:
                    SendContactEventData evtData = evt.GetData<SendContactEventData>();
                    string onlineContactUsername = evtData.Username;
                    MessageBox.Show("Contact " + onlineContactUsername + " is now online.");
                    ClientAccount onlineContactAccount = Program.allContacts.Find(x => x.Username == onlineContactUsername);
                    onlineContactAccount.IsOnline = true;
                    UpdateContactLB();
                    break;

                case EventTypes.ContactWentOffline:
                    SendContactEventData offlineData = evt.GetData<SendContactEventData>();
                    string offlineContactUsername = offlineData.Username;
                    MessageBox.Show("Contact " + offlineContactUsername + " is now offline");
                    ClientAccount offlineContactAccount = Program.allContacts.Find(x => x.Username == offlineContactUsername);
                    offlineContactAccount.IsOnline = false;
                    UpdateContactLB();
                    break;

                case EventTypes.SendAllContacts:
                    SendAllContactsEventData sendContactEvtData = evt.GetData<SendAllContactsEventData>();
                    foreach (string i in sendContactEvtData.AllContacts)
                    {
                        ClientAccount tempCAcc = new ClientAccount(i);
                        Program.allContacts.Add(tempCAcc);
                    }
                    if (this.InvokeRequired) this.Invoke(new Action(this.UpdateContactLB));
                    else this.UpdateContactLB();
                    break;

                case EventTypes.JoinedChatEvent:
                    JoinChatroomEventData joinChatEvtData = evt.GetData<JoinChatroomEventData>();
                    Program.tempChatID = joinChatEvtData.id;
                    Program.clientState = Program.ClientStates.addChatroom;
                    Program.SwitchForm(this);
                    break;
            }
        }

        public bool ShowYesNoDialogBox()
        {
            //Use this for things like "PersonA wants to be a contact. Add them?"
            //or "PersonA wants you to join a chatroom. Join?"
            DialogResult dialogResult = MessageBox.Show("Do you want to do a thing", "501 Chat", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                return true;
            }
            return false;
        }

        private void uxTxtUsername_TextChanged(object sender, EventArgs e)
        {

        }

        private void uxTxtUsername_MouseUp(object sender, MouseEventArgs e)
        {
            uxTxtUsername.Text = "";
        }

        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            LogoutEventData data = new LogoutEventData(Program.USERNAME);
            Event evt = new Event(data, EventTypes.LogoutEvent);
            Program.networkHandler.SendToServer(evt);
            Program.clientState = Program.ClientStates.exitProgram;
            MessageBox.Show("Thank you. You are now signed out.");
            Program.SwitchForm(this);
        }

        private void MainMenu_Load(object sender, EventArgs e)
        {

        }

        public void SendToServer(Event evt)
        {
            Program.networkHandler.SendToServer(evt);
        }

        public void UpdateContactLB()
        {
            List<string> contactUsernames = new List<string>();
            List<string> offlineUsernames = new List<string>();
            foreach (ClientAccount contact in Program.allContacts)
            {
                if(contact.IsOnline)
                {
                    contactUsernames.Add(contact.Username);
                }
                else
                {
                    offlineUsernames.Add(contact.Username);
                }
                
            }


            uxLBContacts.DataSource = contactUsernames.ToList();
            uxLbOfflineContacts.DataSource = offlineUsernames.ToList();
            this.Invalidate();
        }
    }
}
