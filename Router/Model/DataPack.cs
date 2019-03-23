using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Router.Model
{
    class DataPack
    {
        public int Vi { get; set; }//协议
        public int Type { get; set; }//数据类型
        public string IPAddress { get; set; }//Ip地址
        public string DataOrCmd { get; set; }//数据或者命令
    }
}
