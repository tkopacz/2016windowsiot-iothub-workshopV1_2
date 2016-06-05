using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Test5_ArduinoSerial
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private SerialDevice serialPort = null;
        DataWriter dataWriteObject = null;
        private CancellationTokenSource ReadCancellationTokenSource;

        public MainPage()
        {
            this.InitializeComponent();
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            string aqs = SerialDevice.GetDeviceSelector();
            var dis = await DeviceInformation.FindAllAsync(aqs);
            DeviceInformation entry = (DeviceInformation)dis[1]; //Arduino Leonardo Id = "\\\\?\\USB#VID_2A03&PID_8036#5&3753427a&0&2#{86e0d1e0-8089-11d0-9ce4-08003e301f73}"

            serialPort = await SerialDevice.FromIdAsync(entry.Id);

            // Configure serial settings
            serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
            serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
            serialPort.BaudRate = 9600;
            serialPort.Parity = SerialParity.None;
            serialPort.StopBits = SerialStopBitCount.One;
            serialPort.DataBits = 8;
            serialPort.Handshake = SerialHandshake.None;



            base.OnNavigatedTo(e);
            Switch();
           
        }

        private async void Switch()
        {
            while (true)
            {
                dataWriteObject = new DataWriter(serialPort.OutputStream);
                dataWriteObject.WriteString("E"); //To turn on NeoPixel
                var storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

                UInt32 bytesWritten = await storeAsyncTask;
                if (bytesWritten > 0) { /*OK*/ }
                await Task.Delay(2000);
                dataWriteObject.WriteString("O"); //To turn off NeoPixel
                storeAsyncTask = dataWriteObject.StoreAsync().AsTask();
                bytesWritten = await storeAsyncTask;
                if (bytesWritten > 0) { /*OK*/ }
                await Task.Delay(2000);
            }
        }
    }
}
