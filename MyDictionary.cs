using System;
using System.Collections.Generic;
using System.Text;

namespace CentralServerNET
{
    
    public class MyDictionary : Dictionary<string, List<MyValue>>
    {
        public void Add(string session, string nick, int port)
        {
            MyValue val = new CentralServerNET.MyValue();
            val.nick = nick;
            val.port = port;

            List<MyValue> pt = new List<MyValue>();
            pt.Add(val);

            if (this.ContainsKey(session))
            {
                this[session].Add(val);
            }
            else this.Add(session, pt);
        }
    }
}
