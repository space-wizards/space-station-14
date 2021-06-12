namespace Content.Server.Pressure
{
    public interface IPressureProtection
    {
        public float HighPressureMultiplier { get; }
        public float LowPressureMultiplier { get; }
    }
}
