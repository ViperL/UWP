using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Router
{
    class CmdModel//用于承载指令模型
    {
        public string Cmd { get; set; }
        public string PortNum { get; set; }
        public string Value { get; set; }
    }
}
