﻿using FP_Core;
using FP_Core.Events;
using FP_Server.Controller;
using FP_Server.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace FP_Server.Controller
{
    public delegate void Logger(string message, LoggerMessageTypes type);
    class ServerController 
    {
        private Logger _logger;

        private List<Account> _accounts;
        private List<ChatRoom> _rooms;
        
        public ServerController(Logger logger)
        {
            _accounts = new List<Account>();
            _logger = logger;
        }

        public void OnOpen(ServerSocketBehavior sender)
        {
            _logger("A client has connected to the server", LoggerMessageTypes.None);
        }

        public void OnMessage(ServerSocketBehavior sender, MessageEventArgs e)
        {
            Event evt = JsonConvert.DeserializeObject<Event>(e.Data);

            switch (evt.Type)
            {
                #region Account Handlers
                case EventTypes.CreateAccountEvent:
                    {
                        CreateAccountEventData data = evt.GetData<CreateAccountEventData>();

                        ServerResponseEventData response;
                        try
                        {
                            _CreateAccount(data.Username, data.Password);
                            response = new ServerResponseEventData();

                            _logger("Account with username '" + data.Username+"' was created", LoggerMessageTypes.Success);
                        }
                        catch(ArgumentException err)
                        {
                            response = new ServerResponseEventData(err.Message);

                            _logger("Client attempted to create an account and an error was thrown: "+err.Message, LoggerMessageTypes.Error);
                        }
                        sender.Send(JsonConvert.SerializeObject(new Event(response, EventTypes.ServerResponse)));
                        break;
                    }
                case EventTypes.LoginEvent:
                    {
                        LoginEventData data = evt.GetData<LoginEventData>();

                        ServerResponseEventData response;

                        try
                        {
                            _TryLogin(data.Username, data.Password, sender);
                            response = new ServerResponseEventData();

                            _logger("Account with username '" + data.Username + "' has successfully logged in", LoggerMessageTypes.None);
                        }
                        catch(ArgumentException err)
                        {
                            response = new ServerResponseEventData(err.Message);

                            _logger("Client attempted to login with username '"+ data.Username+"' and an error was thrown: " + err.Message, LoggerMessageTypes.Error);
                        }
                        sender.Send(JsonConvert.SerializeObject(new Event(response, EventTypes.ServerResponse)));

                        break;
                    }
                case EventTypes.AddContactEvent:
                    {
                        SendContactEventData data = evt.GetData<SendContactEventData>();

                        ServerResponseEventData response;

                        try
                        {
                            _AddContact(sender, data.Username);

                            response = new ServerResponseEventData();
                            _logger("Account has added a contact with username '" + data.Username + "' to their contact list", LoggerMessageTypes.None);
                        }
                        catch(ArgumentException err)
                        {
                            response = new ServerResponseEventData(err.Message);

                            _logger("Account attempted to add account with username '" + data.Username + "' and an error was thrown: " + err.Message, LoggerMessageTypes.Error);
                        }
                        sender.Send(JsonConvert.SerializeObject(new Event(response, EventTypes.ServerResponse)));

                        break;
                    }
                case EventTypes.LogoutEvent:
                    {
                        LogoutEventData data = evt.GetData<LogoutEventData>();

                        ServerResponseEventData response;

                        try
                        {

                        }
                        catch(ArgumentException err)
                        {

                        }
                        break;
                    }
                    #endregion
            }
        }

        public void OnClose(ServerSocketBehavior sender, CloseEventArgs e)
        {
            _logger("A client has left the server", LoggerMessageTypes.None);
        }

        #region Account Handling

        private void _CreateAccount(string username, string password)
        {
            if (_accounts.Exists(a => a.Username == username)) throw new ArgumentException("That username is already taken");

            _accounts.Add(new Account(username, password));
        } 

        private void _TryLogin(string username, string password, ServerSocketBehavior socket)
        {
            Account acct = _accounts.Find(a => a.Username == username);
            if(acct == null) throw new ArgumentException("No account with that username exists. Please create an account before logging in");
            if (acct.IsOnline) throw new ArgumentException("User is already logged in");
            else if (acct.Password != password) throw new ArgumentException("Username or password is incorrect");

            acct.IsOnline = true;
            acct.Socket = socket;
            _UpdateOnlineContacts(acct);
        }
        private void _TryLogout(string username)
        {
            Account acct = _accounts.Find(a => a.Username == username);

            if (acct == null) throw new ArgumentException("No account with that username exists. Cannot logout");
            if (!acct.IsOnline) throw new ArgumentException("User is not logged in, so cannot logout");

            acct.IsOnline = false;
            
        }

        private void _UpdateOnlineContacts(Account acct)
        {
            List<IAccount> onlineContacts = acct.Contacts.FindAll(a => a.IsOnline);

            SendContactEventData data = new FP_Core.Events.SendContactEventData(acct.Username);
            Event e = new Event(data, EventTypes.SendContact);
            string eventString = JsonConvert.SerializeObject(e);
            foreach(Account onAcct in onlineContacts)
            {
                onAcct.Socket.Send(eventString);
            }
        }

        private void _AddContact(ServerSocketBehavior sender, string username)
        {
            Account senderAcct = _accounts.Find(a => a.Socket == sender);

            if (senderAcct == null) throw new ArgumentException("Could not determine sender! Please try again");

            Account addContact = _accounts.Find(a => a.Username == username);

            if (addContact == null) throw new ArgumentException("No account with username '" + username + "' exists.");
            if (addContact == senderAcct) throw new ArgumentException("Cannot add yourself as a contact");
            if (addContact.Contacts.Contains(senderAcct) || senderAcct.Contacts.Contains(addContact)) throw new ArgumentException("User is already a contact");

            senderAcct.Contacts.Add(addContact);
            addContact.Contacts.Add(senderAcct);

            _UpdateOnlineContacts(senderAcct);
            _UpdateOnlineContacts(addContact);
        }
        #endregion

    }
}
