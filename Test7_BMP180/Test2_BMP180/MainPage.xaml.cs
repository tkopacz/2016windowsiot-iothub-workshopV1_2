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

namespace Test2_BMP180
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

        DispatcherTimer m_t;
        BMP180 m_bmp180;
        private async void setup()
        {
            m_bmp180 = new BMP180();
            await m_bmp180.InitializeAsync();
            m_t = new DispatcherTimer();
            m_t.Interval = TimeSpan.FromSeconds(5);
            m_t.Tick += M_t_Tick;
            m_t.Start();
        }

        
        private async void M_t_Tick(object sender, object e)
        {
            var r = await m_bmp180.GetSensorDataAsync(Bmp180AccuracyMode.Standard);
            //r.
            //var altitude = await m_bmp180.R.ReadAltitudeAsync(seaLevelPressure);
            //var pressure = await m_bmp180.ReadPreasureAsync();
            //var temperature = await m_bmp180.ReadTemperatureAsync();
            Debug.WriteLine($"Alt:{r.Altitude} m, Press:{r.Pressure} Pa,  Temp:{r.Temperature} deg C");
        }
    }
}
