using Microsoft.Azure.Devices.Client;
using Microsoft.IoT.AdcMcp3008;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Adc;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409


/*
    Messages for receiving
*/

namespace MainIoTApp
{
    /// <summary>
    /// Additional information - passwords etc
    /// Enter correct values!: 
    /// </summary>

    public sealed partial class MainPage : Page
    {
        const string TKConnectionString = "HostName=pltkdpepliot2016.azure-devices.net;DeviceId=D01;SharedAccessKey=CupfKeNmHnuL7gOMsYUX0dxpx0Dyqf0QNHgnp3NgnPo=";
        const string DeviceId = "D01";
    }

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
        DispatcherTimer m_tSPI;

        GpioPin m_blink;
        GpioPinValue m_blinkValue;

        /// <summary>
        /// Sea level pressure
        /// </summary>
        /// <remarks>
        /// 101.325, as per:
        /// https://en.wikipedia.org/wiki/Atmospheric_pressure
        /// http://www.britannica.com/science/atmospheric-pressure
        /// </remarks>
        const float seaLevelPressure = 1013.25f;
        BMP280 m_bmp280;

        AdcController m_adc;
        AdcChannel[] m_adcChannel;

        TCS34725 m_tcs;

        //-----------------------------------------------------------------------
        //For Arduino Leonardo - COM
        SerialDevice m_serialPort = null;
        DataWriter m_dataWriteObject;

        //-----------------------------------------------------------------------
        //For Stepper
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

        /// <summary>
        /// Client for IoT Hub SDK
        /// </summary>
        DeviceClient m_clt;
        /// <summary>
        /// Cache for message with ADC info
        /// </summary>
        MSPI m_mSPI;
        /// <summary>
        /// Total number of messages (SPI, MQTT)
        /// </summary>
        int m_msgSpiCount = 0;

        /// <summary>
        /// Total number of messages (All, IoT Client Lib)
        /// </summary>
        int m_msgCount = 0;

        int MaxMsgCount = 1000;

        private async void setup()
        {
            try
            {
                //0.IoTHub client
                m_clt = DeviceClient.CreateFromConnectionString(TKConnectionString, TransportType.Http1);
                await m_clt.SendEventAsync(new Message(new byte[] { 1, 2, 3 }));

                //0. Cache for message
                m_mSPI = new MSPI();
                m_mSPI.DeviceName = DeviceId;
                m_mSPI.MsgType = "SPI";

                //1. LED
                var gpio = GpioController.GetDefault();
                if (gpio != null)
                {
                    m_blink = gpio.OpenPin(26); //See board, connected to pin 19, 
                    m_blinkValue = GpioPinValue.High;
                    m_blink.Write(m_blinkValue);
                    m_blink.SetDriveMode(GpioPinDriveMode.Output);
                }
                //2. BMP280
                m_bmp280 = new BMP280();
                await m_bmp280.Initialize();
                //3. ADC
                m_adc = (await AdcController.GetControllersAsync(AdcMcp3008Provider.GetAdcProvider()))[0];
                m_adcChannel = new AdcChannel[m_adc.ChannelCount];
                for (int i = 0; i < m_adc.ChannelCount; i++)
                {
                    m_adcChannel[i] = m_adc.OpenChannel(i);
                }
                //4. TCS34725
                m_tcs = new TCS34725();
                await m_tcs.Initialize();


                m_t = new DispatcherTimer();
                m_t.Interval = TimeSpan.FromMilliseconds(5000);
                m_t.Tick += M_t_Tick;

                m_tSPI = new DispatcherTimer();
                m_tSPI.Interval = TimeSpan.FromMilliseconds(1000); //Caution - we have 8 000 messages / day. So - LIMIT RATE
                m_tSPI.Tick += M_tSPI_Tick;

                //5. Serial i Arduino (Leonardo)
                string aqs = SerialDevice.GetDeviceSelector();
                var dis = await DeviceInformation.FindAllAsync(aqs);
                DeviceInformation entry = null;
                foreach(var e in dis)
                {
                    //Arduino Leonardo Id = "\\\\?\\USB#VID_2A03&PID_8036#5&3753427a&0&2#{86e0d1e0-8089-11d0-9ce4-08003e301f73}"
                    if (e.Id== "\\\\?\\USB#VID_2A03&PID_8036#5&3753427a&0&2#{86e0d1e0-8089-11d0-9ce4-08003e301f73}")
                    {
                        entry = e;
                        break;
                    }
                }
                if (entry != null)
                {
                    m_serialPort = await SerialDevice.FromIdAsync(entry.Id);

                    // Configure serial settings
                    m_serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                    m_serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                    m_serialPort.BaudRate = 9600;
                    m_serialPort.Parity = SerialParity.None;
                    m_serialPort.StopBits = SerialStopBitCount.One;
                    m_serialPort.DataBits = 8;
                    m_serialPort.Handshake = SerialHandshake.None;
                    m_dataWriteObject = new DataWriter(m_serialPort.OutputStream);
                }

                //6. Stepper & GPIO
                pinsStepper1 = new GpioPin[4];
                for (int i = 0; i <= 3; i++)
                {
                    pinsStepper1[i] = gpio.OpenPin(pins[i]);
                    pinsStepper1[i].SetDriveMode(GpioPinDriveMode.Output);
                }


                //7. Receive
                Task.Run(() => ReceiveDataFromAzure()); //Loop. 


                //Can enable UI
                txtAll.IsEnabled = txtSPI.IsEnabled = tgSend.IsEnabled = true;
                tgSend.IsOn = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                txtState.Text = ex.ToString();
            }


        }

        private async void M_tSPI_Tick(object sender, object e)
        {
            if (m_msgSpiCount >= MaxMsgCount && MaxMsgCount != -1)
            {
                //No more than MaxMsgCount messages / run
                m_tSPI.Stop(); return;
            }
            m_mSPI.Potentiometer1 = m_adcChannel[0].ReadRatio();
            m_mSPI.Potentiometer2 = m_adcChannel[1].ReadRatio();
            m_mSPI.Light = m_adcChannel[2].ReadRatio();
            m_mSPI.Dt = DateTime.UtcNow;
            var obj = JsonConvert.SerializeObject(m_mSPI);
            try
            {
                if (m_clt!=null)
                {
                    await m_clt.SendEventAsync(new Message(System.Text.Encoding.UTF8.GetBytes(obj)));
                    m_msgSpiCount++;
                }
            }
            catch (Exception ex)
            {
                txtState.Text = ex.ToString();
            }

        }
        private async void M_t_Tick(object sender, object e)
        {
            if (m_msgCount >= MaxMsgCount && MaxMsgCount != -1)
            {
                //No more than MaxMsgCount messages / run
                m_t.Stop(); return;
            }
            MAll m = new MAll();
            m.DeviceName = DeviceId;
            m.MsgType = "ALL";

            m.Altitude = await m_bmp280.ReadAltitudeAsync(seaLevelPressure);
            m.Pressure = await m_bmp280.ReadPreasureAsync();
            m.Temperature = await m_bmp280.ReadTemperatureAsync();

            m.Potentiometer1 = m_adcChannel[0].ReadRatio();
            m.Potentiometer2 = m_adcChannel[1].ReadRatio();
            m.Light = m_adcChannel[2].ReadRatio();
            m.ADC3 = m_adcChannel[3].ReadRatio();
            m.ADC4 = m_adcChannel[3].ReadRatio();
            m.ADC5 = m_adcChannel[3].ReadRatio();
            m.ADC6 = m_adcChannel[3].ReadRatio();
            m.ADC7 = m_adcChannel[3].ReadRatio();

            m.ColorRgb = await m_tcs.GetRgbDataAsync();
            m.ColorRaw = await m_tcs.GetRawDataAsync();
            m.ColorName = await m_tcs.GetClosestColorAsync();

            m.Dt = DateTime.UtcNow;
            var obj = JsonConvert.SerializeObject(m);
            try
            {   if (m_clt != null)
                {
                    await m_clt.SendEventAsync(new Message(System.Text.Encoding.UTF8.GetBytes(obj)));
                    m_msgCount++;
                }
                txtState.Text = obj + $", MSG:{m_msgCount}, MSGSPI:{m_msgSpiCount}";
            }
            catch (Exception ex)
            {
                txtState.Text = ex.ToString();
            }
        }

        /// <summary>
        /// Send stepper sequence
        /// </summary>
        public void doStep()
        {
            for (int j = 0; j < 4; j++)
            {
                if (m_Seq[idx, j] == 0)
                {
                    pinsStepper1[j].Write(GpioPinValue.Low);
                }
                else
                {
                    pinsStepper1[j].Write(GpioPinValue.High);
                }
            }
        }

        private void tgSend_Toggled(object sender, RoutedEventArgs e)
        {
            if (tgSend.IsOn)
            {
                m_t.Start();
                m_tSPI.Start();
            }
            else
            {
                m_t.Stop();
                m_tSPI.Stop();
            }
        }

        private void txtSPI_TextChanged(object sender, TextChangedEventArgs e)
        {
            double val;
            if (double.TryParse(txtSPI.Text, out val) && val > 0)
            {
                m_tSPI.Interval = TimeSpan.FromMilliseconds(val);
            }
        }

        private void txtAll_TextChanged(object sender, TextChangedEventArgs e)
        {   
            double val;
            if (double.TryParse(txtAll.Text, out val) && val > 0)
            {
                m_t.Interval = TimeSpan.FromMilliseconds(val);
            }
        }

        public async Task ReceiveDataFromAzure()
        {

            Message receivedMessage;
            string messageData;
            if (m_clt != null)
            {
                while (true)
                {
                    try
                    {
                        receivedMessage = await m_clt.ReceiveAsync();

                        if (receivedMessage != null)
                        {
                            messageData = System.Text.Encoding.ASCII.GetString(receivedMessage.GetBytes());
                            //Do operation
                            if (messageData.Length >= 2)
                            {
                                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                                {
                                    double val;
                                    switch (messageData[0])
                                    {
                                        #region Light (diode)
                                        case 'L':
                                            //Light
                                            if (messageData[1] == '0')
                                                m_blinkValue = GpioPinValue.High;
                                            else
                                                m_blinkValue = GpioPinValue.Low;
                                            m_blink.Write(m_blinkValue);
                                            break;
                                        #endregion
                                        #region Telemetry - parameters
                                        case 'A':
                                            //All messages - interval
                                            if (double.TryParse(messageData.Substring(1), out val))
                                            {
                                                txtAll.Text = val.ToString();
                                            }
                                            break;
                                        case 'S':
                                            //SPI messages - interval
                                            if (double.TryParse(messageData.Substring(1), out val))
                                            {
                                                txtSPI.Text = val.ToString();
                                            }
                                            break;
                                        case 'O':
                                            //On / Off
                                            if (messageData[1] == '0')
                                                tgSend.IsOn = false;
                                            else
                                                tgSend.IsOn = true;
                                            break;
                                        #endregion
                                        #region Neopixel                                        
                                        case 'N':
                                            //Neopixel through On / Off
                                            if (messageData[1] == '1')
                                            {
                                                m_dataWriteObject.WriteString("E"); //To turn on NeoPixel
                                            }
                                            else
                                            {
                                                m_dataWriteObject.WriteString("O"); //To turn off NeoPixel
                                            }
                                            await m_dataWriteObject.StoreAsync();
                                            break;
                                        #endregion
                                        #region Stepper
                                        case 'Q':
                                            //Stepper LEFT
                                            int.TryParse(messageData.Substring(1), out repeatSteps);
                                            for (int i = 0; i < repeatSteps; i++)
                                            {
                                                idx += m_StepDir;
                                                if (idx >= m_StepCount) idx -= m_StepCount;
                                                if (idx < 0) idx += m_StepCount;
                                                doStep();
                                                Task.Delay(10).Wait();
                                            }
                                            break;
                                        case 'W':
                                            int.TryParse(messageData.Substring(1), out repeatSteps);
                                            //Stepper RIGHT
                                            for (int i = 0; i < repeatSteps; i++)
                                            {
                                                idx -= m_StepDir;
                                                if (idx >= m_StepCount) idx -= m_StepCount;
                                                if (idx < 0) idx += m_StepCount;
                                                doStep();
                                                Task.Delay(10).Wait();
                                            }
                                            break;
                                        #endregion
                                        default:
                                            break;
                                    }
                                });
                            }
                            //await m_clt.RejectAsync(receivedMessage);
                            //await m_clt.AbandonAsync(receivedMessage); - reject, will be redelivered
                            //Confirm
                            await m_clt.CompleteAsync(receivedMessage); //commit receive & process
                        }
                    }
                    catch (Exception ex)
                    {
                        txtState.Text = ex.ToString();
                    }
                }
            }
        }


    }
}
