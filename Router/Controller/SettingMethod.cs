using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Router.Model;

namespace Router.Controller
{
    class SettingMethod
    {
        static FileIO ObjFile = new FileIO("Setting.init");
        public async static Task<bool> IsFirstUse()
        {
            if (await ObjFile.PoPOut() == null)
                return true;
            else
                return false;
        }

        public static void CreateSetFile()
        {
            ObjFile.CreateTxt();
        }
        public static void SetParameter(SettingModel Para)
        {
            string Data = Para.AzureID + "/" + Para.AzurePSW;
            ObjFile.UpdateSet(Data);
        }

        public async static Task<SettingModel> GetParameter()
        {
            SettingModel ObjSet = new SettingModel();
            string[] DataArray = await ObjFile.PoPOut();
            string[] data = null;//用于存放问题
            try
            {
                foreach (string item in DataArray)//切割出数据
                {
                    data = item.Trim().Split(Convert.ToChar("/"));
                    if (data[0] == string.Empty)
                        break;
                    ObjSet.AzureID = data[0].Trim();
                    ObjSet.AzurePSW = data[1].Trim();
                }
            }
            catch (Exception)
            { }
            return ObjSet;
        }
    }
}
