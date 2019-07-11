using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Router.Model;

namespace Router
{
    class ModelConverter//用于视觉模型和数据模型的转换
    {
        public static ViewDevModel DataToView(DeviceModel item)
        {
            string ImageUrl;
            switch (item.Type)
            {
                case "温度": ImageUrl = "/Source/Temp.png"; break;
                case "湿度": ImageUrl = "/Source/humidity.png"; break;
                case "Lumia 930": ImageUrl = "/Source/phone.png"; break;
                case "AirConditioner": ImageUrl = "/Source/airconditioner.png"; break;
                case "照明系统": ImageUrl = "/Source/Lightpng.png"; break;
                default:ImageUrl = null;break;
            }
            ViewDevModel Obj = new ViewDevModel()
            {
                NetPort = item.NetPort,
                Type = item.Type,
                IPAddress = item.IPAddress,
                ImageUrl = ImageUrl
            };
            return Obj;
        }

        public static DeviceModel ViewToData(ViewDevModel ViewInfo)
        {
            DeviceModel Info = new DeviceModel() { NetPort = ViewInfo.NetPort, IPAddress = ViewInfo.IPAddress, Type = ViewInfo.Type };
            return Info;
        }
    }
}
