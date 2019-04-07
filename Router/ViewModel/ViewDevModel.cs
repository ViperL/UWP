using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Router
{
    class ViewDevModel
    {
        public int NetPort { get; set; }//端口号-代ID
        public string IPAddress { get; set; }//IP地址
        public string Type { get; set; }//设备型号
        public string ImageUrl { get; set; }//图标接口
    }
}
