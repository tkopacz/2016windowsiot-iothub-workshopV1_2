using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Adc;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//Download ADC Providers from: https://github.com/ms-iot/BusProviders.git
using Microsoft.IoT.AdcMcp3008;
using System.Diagnostics;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409
namespace Test3_MCP3008
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

        private AdcController m_adc;
        private AdcChannel[] m_adcChannel;
        DispatcherTimer m_t;
        private async void setup()
        {
            m_adc = (await AdcController.GetControllersAsync(AdcMcp3008Provider.GetAdcProvider()))[0];
            m_adcChannel = new AdcChannel[m_adc.ChannelCount];
            for (int i = 0; i < m_adc.ChannelCount; i++)
            {
                m_adcChannel[i] = m_adc.OpenChannel(i);
            }
            m_t = new DispatcherTimer();
            m_t.Interval = TimeSpan.FromSeconds(5);
            m_t.Tick += M_t_Tick;
            m_t.Start();
        }

        private void M_t_Tick(object sender, object e)
        {
            for (int i = 0; i < m_adc.ChannelCount; i++)
            {
                Debug.Write($"{i}:{m_adcChannel[i].ReadRatio():F4}, ");
            }
            Debug.WriteLine("");
        }
    }
}
