using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

namespace DemoStepperApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            Setup();
        }

        byte[,] m_Seq = new byte[,] {
            {1, 0, 0, 0},
            {1, 1, 0, 0},
            {0, 1, 0, 0},
            {0, 1, 1, 0},
            {0, 0, 1, 0},
            {0, 0, 1, 1},
            {0, 0, 0, 1},
            {1, 0, 0, 1}
        };


        int m_StepCount = 8;
        int m_StepDir = 1; //Set to 1 or 2 for clockwise | Set to -1 or -2 for anti-clockwise

        //PINs on stepper driver:    1  2  3  4
        //GPIO on RPI           :    5  6 13 19  
        //PINs on RPI2          :   29 31 33 35
        //https://developer.microsoft.com/en-us/windows/iot/win10/samples/pinmappingsrpi2
        int[] pins = new int[4] { 5, 6, 13, 19 };
        GpioPin[] pinsStepper1;
        int idx = 0;
        int repeatSteps = 200;
        public async void Setup()
        {
            var gpio = GpioController.GetDefault();
            pinsStepper1 = new GpioPin[4];
            for (int i = 0; i <= 3; i++)
            {
                pinsStepper1[i] = gpio.OpenPin(pins[i]);
                pinsStepper1[i].SetDriveMode(GpioPinDriveMode.Output);
            }

        }

        private void cmdRight_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < repeatSteps; i++)
            {
                idx -= m_StepDir;
                if (idx >= m_StepCount) idx -= m_StepCount;
                if (idx < 0) idx += m_StepCount;
                doStep();
                Task.Delay(10).Wait();
            }
        }

        private void cmdLeft_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < repeatSteps; i++)
            {
                idx += m_StepDir;
                if (idx >= m_StepCount) idx -= m_StepCount;
                if (idx < 0) idx += m_StepCount;
                doStep();
                Task.Delay(10).Wait();
            }
        }

        public void doStep()
        {
            for (int j = 0; j < 4; j++)
            {
                if (m_Seq[idx, j] == 0)
                {
                    pinsStepper1[j].Write(GpioPinValue.Low);
                }
                else {
                    pinsStepper1[j].Write(GpioPinValue.High);
                }
            }
        }
    }
}
