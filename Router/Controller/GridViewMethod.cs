using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Router.Model;

namespace Router.Controller
{
    class GridViewMethod
    {
        public static void LayOutItem(GridView gridView,List<ViewDevModel> Obj)
        {
            foreach (var item in Obj)
            {
                //gridView.Items.Add(new DeviceModel { NetPort = item.NetPort, Type = item.Type , IPAddress = item.IPAddress });
                gridView.Items.Add(new ViewDevModel { NetPort = item.NetPort, Type = item.Type, IPAddress = item.IPAddress,ImageUrl= item.ImageUrl });
            }
        }
    }
}
