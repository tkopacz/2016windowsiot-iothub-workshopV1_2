using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Gpio;
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

namespace Test1_BlinkingLed
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
        GpioPin m_blink;
        GpioPinValue m_blinkValue;
        private async void setup()
        {
            var gpio = GpioController.GetDefault();
            if (gpio!=null)
            {
                m_blink = gpio.OpenPin(26); //See board, connected to pin 19, 
                m_blinkValue = GpioPinValue.High;
                m_blink.Write(m_blinkValue);
                m_blink.SetDriveMode(GpioPinDriveMode.Output);
                m_t = new DispatcherTimer();
                m_t.Interval = TimeSpan.FromSeconds(1);
                m_t.Tick += M_t_Tick;
                m_t.Start();
            }
        }

        private void M_t_Tick(object sender, object e)
        {
            if (m_blinkValue == GpioPinValue.High) m_blinkValue = GpioPinValue.Low; else m_blinkValue = GpioPinValue.High;
            m_blink.Write(m_blinkValue);
        }
    }
}
