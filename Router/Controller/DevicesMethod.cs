using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using Router.Controller;
using Router.Model;

namespace Router
{
    class DevicesMethod
    {
        static FileIO ObjFile = new FileIO("DevLog.log");

        public async static  Task<int>  GetPortNum()//获取端口号
        {
            int PortNum = 0;
            List<DeviceModel> ObjList = await GetDevices();
            try
            { PortNum = ObjList[ObjList.Count - 1].NetPort; }
            catch (Exception) { PortNum = 9000; }//初始端口
            return PortNum;
        }

        public async static Task<bool> IsFirstUse()
        {
            if (await ObjFile.PoPOut() == null)
                return true;
            else
                return false;
        }//文件是否丢失

        public static void CreateLog()
        {
            ObjFile.CreateTxt();
        }//创建文件

        public static void UpdateDevices(List<DeviceModel> ObjDev)//升级数据
        {
            ObjFile.AddDevices(ObjDev);
        }

        public async static Task<List<DeviceModel>> GetDevices()
        {
            List<DeviceModel> ListDev = new List<DeviceModel>();
            string[] DataArray = await ObjFile.PoPOut();
            string[] data = null;
            try
            {
                foreach (string item in DataArray)//切割出数据
                {
                    data = item.Trim().Split(Convert.ToChar("/"));
                    if (data[0] == null)
                        break;
                    ListDev.Add(new DeviceModel
                    {
                        Type = data[0].Trim(),
                        IPAddress = data[1].Trim(),
                        NetPort = Convert.ToInt16(data[2].Trim()),
                    });
                }
            }
            catch (Exception)
            {}
            return ListDev;
        }//获取设备列表

    }
}
