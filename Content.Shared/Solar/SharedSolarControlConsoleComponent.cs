using Robust.Shared.Serialization;

namespace Content.Shared.Solar
{
    [Serializable, NetSerializable]
    public sealed class SolarControlConsoleBoundInterfaceState : BoundUserInterfaceState
    {
        /// <summary>
        /// The target rotation of the panels in radians.
        /// </summary>
        public Angle Rotation;

        /// <summary>
        /// The target velocity of the panels in radians/minute.
        /// </summary>
        public Angle AngularVelocity;

        /// <summary>
        /// The total amount of power the panels are supplying.
        /// </summary>
        public float OutputPower;

        /// <summary>
        /// The current sun angle.
        /// </summary>
        public Angle TowardsSun;

        public SolarControlConsoleBoundInterfaceState(Angle r, Angle vm, float p, Angle tw)
        {
            Rotation = r;
            AngularVelocity = vm;
            OutputPower = p;
            TowardsSun = tw;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SolarControlConsoleAdjustMessage : BoundUserInterfaceMessage
    {
        /// <summary>
        /// New target rotation of the panels in radians.
        /// </summary>
        public Angle Rotation;

        /// <summary>
        /// New target velocity of the panels in radians/second.
        /// </summary>
        public Angle AngularVelocity;
    }

    [Serializable, NetSerializable]
    public enum SolarControlConsoleUiKey
    {
        Key
    }
}
