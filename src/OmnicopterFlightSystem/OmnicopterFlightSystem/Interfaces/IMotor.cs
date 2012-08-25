namespace Omnicopter.Interfaces
{
    interface IMotor
    {
        /// <summary>
        /// Sets the motor power to a specific power level.
        /// </summary>
        /// <param name="level">The power level to set the motor to.</param>
        void SetPower(int level);

        /// <summary>
        /// Adjust the power level up or down.
        /// </summary>
        /// <param name="adjustment">The amount to increase or decrease the current power level.</param>
        void AdjustPower(int adjustment);

        /// <summary>
        /// Set the scale value that gets applied to all motors.  Used for adjusting altitude.
        /// </summary>
        /// <param name="scale">The factor to multiply all power levels by.</param>
        void SetScale(uint scale);

        /// <summary>
        /// Get the current power level.
        /// </summary>
        /// <returns>The current power level.</returns>
        int GetPowerLevel();
    }
}
