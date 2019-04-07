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

using System.Threading;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace Router
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class WelcomePage : Page
    {
        private Timer periodicTimer;
        int Count = 0;
        public WelcomePage()
        {
            periodicTimer = new Timer(this.TimerCallback, null, 0, 100);
            this.InitializeComponent();
        }

        private void TimerCallback(object state)//定时器调用
        {
            if (Count == 30)
            {
                var task = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Frame.Navigate(typeof(MainPage));
                    periodicTimer.Dispose();
                });//解除线程隔阂(UI线程中刷新数据)
            }
            Count++;
        }
    }
}
