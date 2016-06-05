using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainIoTApp
{
    public class MIoTBase
    {
        public DateTime Dt { get; set; }
        public string MsgType { get; set; }
        public string DeviceName { get; set; }
    }
    public class MSPI:MIoTBase
    {
        public double Potentiometer1 { get; set; }
        public double Potentiometer2 { get; set; }
        public double Light { get; set; }
    }
    public class MAll : MSPI
    {
        public double ADC3 { get; internal set; }
        public double ADC4 { get; internal set; }
        public double ADC5 { get; internal set; }
        public double ADC6 { get; internal set; }
        public double ADC7 { get; internal set; }
        public float Altitude { get; internal set; }
        public string ColorName { get; internal set; }
        public ColorData ColorRaw { get; internal set; }
        public RgbData ColorRgb { get; internal set; }
        public float Pressure { get; internal set; }
        public float Temperature { get; internal set; }
    }
}
