using System;
using System.Collections.Generic;
using System.Diagnostics;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Test4_TCS34725
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            setup();
        }
        TCS34725 m_tcs;
        DispatcherTimer m_t;

        private async void setup()
        {
            m_tcs = new TCS34725();
            await m_tcs.Initialize();
            m_t = new DispatcherTimer();
            m_t.Interval = TimeSpan.FromSeconds(1);
            m_t.Tick += M_t_Tick;
            m_t.Start();
        }

        private async void M_t_Tick(object sender, object e)
        {
            var color = await m_tcs.GetRgbDataAsync();
            Debug.WriteLine($"{color.Red},{color.Green},{color.Blue}");
        }
    }
}
