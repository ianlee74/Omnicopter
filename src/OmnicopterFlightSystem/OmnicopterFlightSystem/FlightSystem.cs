using System.IO.Ports;
using System.Text;
using System.Threading;
using GHIElectronics.NETMF.FEZ;
using GHIElectronics.NETMF.Hardware;
using Microsoft.SPOT;
using NETMFx.Math;
using NETMFx.Wireless;
using NETMFx.Hardware;
using Omnicopter.Interfaces;

namespace Omnicopter
{
    public enum  FrontOrientationStyle
    {
        T,      // One motor leads
        X       // Two motors lead
    }

    public enum FlightMode
    {
        FreeStyle,  // Do whatever commands you receive from the remote.
        Hover       // Hover in a balanced state at the current location and altitude. 
    }

    public class FlightSystem
    {
        private readonly IMotor[] _propellors = new IMotor[4];
// ReSharper disable RedundantArgumentDefaultValue
        private readonly RCRadio _radio = new RCRadio("OC1", "COM1", 115200, Parity.None, 8, StopBits.One);
// ReSharper restore RedundantArgumentDefaultValue
        private EuclideanVector _flightVector;

        private static readonly SerialPort _imuSerialPort = new SerialPort("COM3", 115200, Parity.None, 8, StopBits.One);
        private static readonly CKMongooseImu _imu = new CKMongooseImu();
        
        private static FrontOrientationStyle _frontOrientationStyle;
        private static FlightMode _flightMode;

        private static int _powerBase = 25;

        public uint MotorScale;
        
        public FlightSystem()
        {
            _frontOrientationStyle = FrontOrientationStyle.T;
            _flightMode = FlightMode.Hover;

            _radio.Id = "OC1";
            _radio.PartnerId = "OM1";
            _radio.SendFrequency = 100;
            MotorScale = 100;
            const Period periodLength = Period.P50Hz;

            RegisterPropeller(new BrushlessMotor((PWM.Pin) FEZ_Pin.PWM.Di6, periodLength));           // 3:00 position
            RegisterPropeller(new BrushlessMotor((PWM.Pin) FEZ_Pin.PWM.Di5, periodLength));           // 12:00 (green)
            RegisterPropeller(new BrushlessMotor((PWM.Pin) FEZ_Pin.PWM.Di9, periodLength));           // 9:00 position
            RegisterPropeller(new BrushlessMotor((PWM.Pin) FEZ_Pin.PWM.Di8, periodLength));           // 6:00 position
        }

        public void Start()
        {
            // Wait 2 seconds for the ESCs to arm.
            Thread.Sleep(2000);

            Debug.EnableGCMessages(true);

            // Activate the radio and start receiving data.
//            _radio.DataReceived += OnRadioDataReceived;
//            _radio.DataProcessor = OnRadioDataReceived;
//            _radio.Activate(true);

            /*
            while (true)
            {
                _propellors[2].SetPower(20);
                //Thread.Sleep(1);
                _propellors[2].SetPower(60);
                //Thread.Sleep(1);
            }
            */
            _imuSerialPort.Open();
            _imuSerialPort.DataReceived += OnImuSerialPortDataReceived;

            var motorThread = new Thread(MonitorFlightVector);
            motorThread.Start();


            /*
            _propellors[1].SetPower(80);
            Thread.Sleep(2000);
            _propellors[1].SetPower(0);

            foreach (var propellor in _propellors)
            {
                if (propellor == null) return;
                propellor.SetPower(20);
                Thread.Sleep(2000);
                propellor.SetPower(0);
            }
*/
            Thread.Sleep(Timeout.Infinite);
        }

        private static string _imuBuffer = "";
        private static string _imuLastSentence = "";
        private static int _imuBytesReceived;

        private static void OnImuSerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            const string START_MARKER = "!ANG:";

            Thread.Sleep(20);
            // Read all data and buffer to a string.
            if (!_imuSerialPort.IsOpen) return;
            _imuBytesReceived = ((SerialPort) sender).BytesToRead;
            var bytes = new byte[_imuBytesReceived];
            ((SerialPort) sender).Read(bytes, 0, _imuBytesReceived);
            var strBuffer = new string(Encoding.UTF8.GetChars(bytes));
            // See if the buffer contains a start marker.
            if (strBuffer == null) return;
            var start = strBuffer.IndexOf(START_MARKER);
            if(start >= 0)
            {
                if (_imuBuffer == "")
                {
                    // Assumes there are not more than one start marker in a single message.
                    _imuBuffer = strBuffer.Substring(start, strBuffer.Length - start);
                }
                else
                {
                    if (start > 0)          // start marker is not the first thing in the message.
                    {
                        _imuBuffer += strBuffer.Substring(0, start);
                    }
                    _imuLastSentence = _imuBuffer;
                    _imuBuffer = "";
                }
            }
#if DEBUG
//            Debug.Print(_imuLastSentence);
#endif
        }

        private void OnRadioDataReceived(object sender, RadioDataReceivedEventArgs e)  //string[] data)
        {
#if DEBUG1
            var d1 = "RECEIVED:  ";
            foreach (var d in data)
            {
                d1 += (d.Length > 11 ? "|" : "") + d;
            }
            Debug.Print(d1);
            Thread.Sleep(400);
#endif
            var data = e.Data;
            switch (data[0])
            {
                case "M":
                    var propId = byte.Parse(data[1]);
                    if (_propellors[propId] != null)
                    {
                        var powerLevel = int.Parse(data[2]);
                        _propellors[propId].SetPower(powerLevel);
                        Thread.Sleep(2000);
                        _propellors[propId].SetPower(0);
                    }
                    break;
                case "D":
                    if (data.Length != 4) break;
                    var direction = (float)double.Parse(data[1]);
                    var magnitude = (float)double.Parse(data[2]);
                    if ( _flightVector == null || (MathEx.Abs((_flightVector.Direction.Radians - direction)) > 1E-10 && MathEx.Abs(_flightVector.Magnitude - magnitude) > 1E-10))
                    {
                        _flightVector = new EuclideanVector(new Angle(direction), magnitude);
                    }
                    break;
            }
            return;
        }

        private void MonitorFlightVector()
        {
            while (true)
            {
                if (_imuLastSentence != null && _imuLastSentence != "")
                {
                    if (!_imu.SetData(_imuLastSentence)) continue;
                    FlightAdjustment fa;
                    switch (_flightMode)
                    {
                        case FlightMode.FreeStyle:
                            fa = FreeStyle();
                            break;
                        default:
                            fa = Hover();
                            break;
                    }
#if(DEBUG)
//                    Debug.Print("Flight Adjustment: roll = " + fa.Roll + " pitch = " + fa.Pitch);
#endif
                    AdjustFlight(fa);
                }
                Thread.Sleep(0);
            }
        }

        private FlightAdjustment FreeStyle()
        {
            var fa = new FlightAdjustment();
            if (_flightVector == null) return fa;
// TODO:  Rewrite to return an FA instead of powering directly.
            /*
            var quadrant = _flightVector.RelativeQuadrant;
            if (quadrant > 0) quadrant--;
            for (byte propId = 0; propId < 4; propId++)
            {
                var power = propId == quadrant ? (int) _flightVector.Magnitude : 0;
                _propellors[propId].SetPower(power);
            }
             */
            return fa;
        }

        private void AdjustFlight(FlightAdjustment adjustment)
        {
            const int MAX_ADJUSTMENT = 2;

            // Roll
            var adjAmt = (int) adjustment.Roll / 2;
            if (adjAmt == 0)
            {
                _propellors[1].SetPower(_powerBase);
                _propellors[3].SetPower(_powerBase);
                return;
            }
            if (adjAmt > MAX_ADJUSTMENT) adjAmt = MAX_ADJUSTMENT;
            if (adjAmt < MAX_ADJUSTMENT * (-1)) adjAmt = MAX_ADJUSTMENT * (-1);
            _propellors[1].SetPower(_powerBase + adjAmt);
            _propellors[3].SetPower(_powerBase - adjAmt);

#if DEBUG
            Debug.Print("#1: " + _propellors[1].GetPowerLevel() + "   #2: " + _propellors[3].GetPowerLevel());
#endif
/*
            // Pitch
            var pitchDirection = 0;
            if (adjustment.Pitch > 0) pitchDirection = 1;
            if (adjustment.Pitch < 0) pitchDirection = -1;
            adjAmt = (int) adjustment.Pitch/2*pitchDirection;
            _propellors[1].AdjustPower(adjAmt);
            _propellors[3].AdjustPower(adjAmt * (-1));
 */
        }

        private struct FlightAdjustment
        {
            public double Roll;
            public double Pitch;
        }

        private FlightAdjustment Hover()
        {
            return new FlightAdjustment() {Roll = _imu.Roll * (-1.0), Pitch = _imu.Pitch * (-1.0)};
        }

        private void RegisterPropeller(IMotor propellor)
        {
            for(byte ndx = 0; ndx < _propellors.Length; ndx++)
            {
                if(_propellors[ndx] == null)
                {
                    _propellors[ndx] = propellor;
                    _propellors[ndx].SetScale(MotorScale);
                    return;
                }
            }
        }
    }
}
