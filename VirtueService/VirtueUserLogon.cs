using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtueService
{
    class VirtueConfigurationEvent
    {
        public enum VirtueEvent { LOGON, LOGOFF, POLL }

        public VirtueConfigurationEvent(string username, VirtueEvent evt)
        {
            User = username;
            ConfigurationEvent = evt;
        }

        public String User
        {
            get;
            set;
        }

        public VirtueEvent ConfigurationEvent
        {
            get;
            set;
        }
    }
}
