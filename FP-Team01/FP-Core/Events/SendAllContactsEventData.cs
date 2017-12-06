﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FP_Core.Events
{
    class SendAllContactsEventData
    {
        private List<IAccount> _allContacts;
        private string _username;

        public SendAllContactsEventData(string u)
        {
            _username = u;
        }

        public List<IAccount> AllContacts
        {
            get
            {
                return _allContacts;
            }
            set
            {
                _allContacts = value;
            }
        }

        public string Username
        {
            get
            {
                return _username;
            }
        }
    }
}
