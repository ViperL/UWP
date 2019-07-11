using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Router
{
    class CmdAnay
    {
        public static CmdModel CmdAnayer(string CmdString)
        {
            string[] data = CmdString.Trim().Split(Convert.ToChar("/"));
            CmdModel ObjItem = new CmdModel()
            {
                Cmd = data[0].Trim(),
                PortNum = data[1].Trim(),
                Value = data[2].Trim(),
            };
            return ObjItem;
        }
    }
}
