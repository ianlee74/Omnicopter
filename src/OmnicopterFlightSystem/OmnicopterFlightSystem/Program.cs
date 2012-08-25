using System;
using System.IO.Ports;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using GHIElectronics.NETMF.Hardware;
using GHIElectronics.NETMF.FEZ;
using NETMFx.Wireless;
using Omnicopter.Interfaces;

namespace Omnicopter
{
    public class Program
    {
        //private static readonly IMotor[] Propellors = new[]{ new BrushlessMotor((PWM.Pin)FEZ_Pin.PWM.Di5), 
        //                                                        new BrushlessMotor((PWM.Pin)FEZ_Pin.PWM.Di6),
        //                                                        new BrushlessMotor((PWM.Pin)FEZ_Pin.PWM.Di8),
        //                                                        new BrushlessMotor((PWM.Pin)FEZ_Pin.PWM.Di9)
        //                                                      };

        //private static readonly RCRadio Radio = new RCRadio("COM1", 115200, Parity.None, 8, StopBits.One);

        public static void Main()
        {
            var fs = new FlightSystem();
            fs.Start();

            // Activate the radio and start receiving data.
            //Radio.DataReceived += OnRadioDataReceived;
            //Radio.Activate(true);

            //foreach (var propellor in Propellors)
            //{
            //    propellor.SetScale(100);
            //}
        }

        //static void OnRadioDataReceived(object sender, RadioDataReceivedEventArgs e)
        //{
        //    Debug.Print(e.Data);
        //    string[] data = e.Data.Split(':');
        //    switch (data[0])
        //    {
        //        case "M":
        //            byte propId = byte.Parse(data[1]);
        //            int powerLevel = int.Parse(data[2]);
        //            Propellors[propId].SetPower(powerLevel);
        //            Thread.Sleep(2000);
        //            Propellors[propId].SetPower(0);
        //            break;
        //    }
        //}
    }
}
