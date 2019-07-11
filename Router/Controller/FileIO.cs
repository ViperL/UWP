using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Threading.Tasks;

using Router.Model;

namespace Router.Controller
{
    class FileIO
    {
        public FileIO(string file)//构造函数
        {
            file_name = file;
        }

        private StorageFile samplie;
        StorageFolder folder = ApplicationData.Current.LocalFolder;//获取应用文件目录
        public string file_name;

        public async void CreateTxt()//创建文件
        {
            samplie = await folder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);
        }

        public async void AddQuene(string Info)
        {
            try
            {
                StorageFile file = await folder.GetFileAsync(file_name);
                if (file != null)
                {
                    string Str = await Windows.Storage.FileIO.ReadTextAsync(file);
                    await Windows.Storage.FileIO.WriteTextAsync(file, Str + Info + "\n");
                }
            }
            catch (Exception)
            { }
        }

        public async void AddDevices(List<DeviceModel> Obj)
        {
            StorageFile file = await folder.GetFileAsync(file_name);
            if (file != null)
            {
                await Windows.Storage.FileIO.WriteTextAsync(file,"");//清空原数据
                try
                {
                    foreach (var item in Obj)
                    {
                        string Data = item.Type + "/" + item.IPAddress + "/" + item.NetPort;
                        string Str = await Windows.Storage.FileIO.ReadTextAsync(file);
                        await Windows.Storage.FileIO.WriteTextAsync(file, Str + Data + "\n");
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public async void UpdateSet(string Data)//用于更新设置
        {
            StorageFile file = await folder.GetFileAsync(file_name);
            if (file != null)
            {
                await Windows.Storage.FileIO.WriteTextAsync(file, "");//清空原数据
                try
                {
                    string Str = await Windows.Storage.FileIO.ReadTextAsync(file);
                    await Windows.Storage.FileIO.WriteTextAsync(file, Str + Data + "\n");
                }
                catch (Exception)
                {
                }
            }
        }

        public async Task<string[]> PoPOut()
        {
            string[] Quene = null;
            try
            {
                StorageFile file = await folder.GetFileAsync(file_name);
                string Str = await Windows.Storage.FileIO.ReadTextAsync(file);
                Quene = Str.Split(Environment.NewLine.ToCharArray());
                return Quene;
                //await Windows.Storage.FileIO.WriteTextAsync(file, "");//清空数据栈
            }
            catch (Exception)
            {
                return Quene;
            }
        }

        public async Task<string[]> PoPToCloud()
        {
            string[] Quene = null;
            try
            {
                StorageFile file = await folder.GetFileAsync(file_name);
                string Str = await Windows.Storage.FileIO.ReadTextAsync(file);
                Quene = Str.Split(Environment.NewLine.ToCharArray());
                await Windows.Storage.FileIO.WriteTextAsync(file, "");//清空数据栈
                return Quene;
            }
            catch (Exception)
            {
                return Quene;
            }
        }

    }
}
