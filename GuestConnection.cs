using System;
using System.Collections.Generic;
using System.Text;

namespace CentralServerNET
{
    class GuestConnection
    {
        public int videoport { get; set; }
        public int audioporttcp { get; set; }
        public int audioportudp { get; set; }
        public string sessionguid { get; set; }
        public string nick { get; set; }
        public int videowidth { get; set; }
        public int videoheight { get; set; }
        public string videocodec { get; set; }

    }
}
