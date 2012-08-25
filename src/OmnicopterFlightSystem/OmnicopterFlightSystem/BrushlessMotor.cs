using System.Threading;
using GHIElectronics.NETMF.Hardware;
using Omnicopter.Interfaces;

namespace Omnicopter
{
    /// <summary>
    /// Class originally created by zerov83 (Chris) at
    /// http://code.tinyclr.com/project/62/brushless-motor-esc-for-aircrafts/
    /// </summary>
    public class BrushlessMotor : IMotor
    {
        protected static uint Period;
        protected const uint Max = 20 * 100 * 1000;
        protected const uint Min = 10 * 100 * 1000;

        // Scale
        private uint _precalc;
        private uint _scale = 100;
        public uint Scale
        {
            get
            {
                return _scale;
            }
            set
            {
                SetPower(0);
                _scale = value;
                _precalc = (Max - Min) / _scale;
            }
        }

        public void AdjustPower(int adjustment)
        {
            SetPower(_powerLevel + adjustment);
        }

        public void SetScale(uint scale)
        {
            Scale = scale;
        }

        private int _powerLevel;
        public int PowerLevel
        {
            get { return _powerLevel; }
            set
            {
                SetPower(value);
            }
        }

        public int GetPowerLevel()
        {
            return PowerLevel;
        }

        private readonly PWM _pwmPin;

        /// <summary>
        /// Uses 50Hz
        /// </summary>
        /// <param name="pin">PWM Pin</param>
        public BrushlessMotor(PWM.Pin pin)
            : this(pin, Omnicopter.Period.P50Hz)
        {
        }

        /// <summary>
        /// Use higher period, means faster response, not supported by all ESC
        /// </summary>
        /// <param name="pin">PWM pin</param>
        /// <param name="period">Period</param>
        public BrushlessMotor(PWM.Pin pin, Period period)
        {
            _precalc = (Max - Min) / _scale;
            Period = (uint)period;
            this._pwmPin = new PWM(pin);
            this._pwmPin.SetPulse(Period, Min);
        }

        /// <summary>
        /// Sets the outputpower, from 0 to "Scale"
        /// </summary>
        /// <param name="power">0..."Scale"</param>
        public void SetPower(int power)
        {
            if (power == PowerLevel) return;    // Don't waste time setting if the power level isn't changing.
            var highTime = (uint) (Min + (_precalc*Constrain(power, 0, (int) _scale)));
            _pwmPin.SetPulse(Period, highTime);
            _powerLevel = power;
        }

        private static int Constrain(int x, int a, int b)
        {
            if (x >= a && x < b)
            {
                return x;
            }
            if (x < a)
            {
                return a;
            }
            return b;
        }
    }

    public enum Period : uint
    {
        /// <summary>
        /// Standard rate 50Hz
        /// </summary>
        P50Hz = 20 * 1000000,
        /// <summary>
        /// Turbo PWM 400Hz
        /// </summary>
        P400Hz = 25 * 100000,
        /// <summary>
        /// Turbo PWM 500Hz
        /// </summary>
        P500Hz = 2 * 1000000,
    }
}
