namespace Content.Server.Interfaces.GameObjects
{
    public interface IPressureProtection
    {
        public float HighPressureMultiplier { get; }
        public float LowPressureMultiplier { get; }
    }
}
