using Microsoft.IoT.AdcMcp3008;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Windows.Devices.Adc;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409


namespace MainIoTApp
{
    /// <summary>
    /// Additional information - passwords etc
    /// Enter correct values!: 
    /// </summary>

    public sealed partial class MainPage : Page
    {
        const string TKMqttDeviceId = "D01";
        const string TKConnectionMqtt = "pltkdpepliot2016.azure-devices.net";
        const string TKConnectionMqttUsername = "pltkdpepliot2016.azure-devices.net/" + TKMqttDeviceId;
        //SAS:

        const string TKConnectionMqttPassword = "SharedAccessSignature sr=pltkdpepliot2016.azure-devices.net&sig=<enter>";
        const string TKMqttTopicSend = "devices/" + TKMqttDeviceId + "/messages/events";

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
        /// <summary>
        /// Client for MqTT
        /// </summary>
        MqttClient m_mqtt;
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

                //0. MQQT for IoT Hub, uPLibrary.Networking.M2Mqtt
                m_mqtt = new MqttClient(TKConnectionMqtt, 8883, true, MqttSslProtocols.TLSv1_2);
                //Device must be registered!
                m_mqtt.Connect(TKMqttDeviceId, TKConnectionMqttUsername, TKConnectionMqttPassword);
                if (m_mqtt.IsConnected == false) throw new ArgumentException("Bad username/password for MQTT");

                //0. Cache for message
                m_mSPI = new MSPI();
                m_mSPI.DeviceName = TKMqttDeviceId;
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
                if (m_mqtt != null)
                {
                    //Publish to internal queue; then - background process send messages to IoT Hub 
                    m_mqtt.Publish(TKMqttTopicSend, System.Text.Encoding.UTF8.GetBytes(obj));
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
            m.DeviceName = TKMqttDeviceId;
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
            {
                if (m_mqtt != null)
                {
                    //Publish to internal queue; then - background process send messages to IoT Hub 
                    m_mqtt.Publish(TKMqttTopicSend, System.Text.Encoding.UTF8.GetBytes(obj));
                    m_msgSpiCount++;
                }
                txtState.Text = obj + $", MSG:{m_msgCount}, MSGSPI:{m_msgSpiCount}";
            }
            catch (Exception ex)
            {
                txtState.Text = ex.ToString();
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
                m_t.Stop();
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

    }
}
