using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Router.Model;
using Router.ViewModel;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace Router
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        List<InfoMdoel> Obj = new List<InfoMdoel>();
        public static MainPage Current;

        public MainPage()
        {
            this.InitializeComponent();
            for (int i = 0; i < 10; i++)
            {
                Obj.Add(new InfoMdoel { ID = i, Type = "温度" + i.ToString(), Statue = "正常" });
            }
            gridView.ItemsSource = Obj;
        }

        private void GridView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {

            var Info = (e.OriginalSource as FrameworkElement)?.DataContext as InfoMdoel;
            var Reslut= Frame.Navigate(typeof(DetailPage), Info);
        }

        private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InfoMdoel ObjSen = new InfoMdoel();
            foreach (var item in e.AddedItems)
            {
                ObjSen = (InfoMdoel)item;
            }
            var Reslut = Frame.Navigate(typeof(DetailPage), ObjSen);
        }
    }
}
