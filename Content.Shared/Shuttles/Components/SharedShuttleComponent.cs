namespace Content.Shared.Shuttles.Components
{
    public abstract class SharedShuttleComponent : Component
    {
        [ViewVariables]
        public virtual bool Enabled { get; set; } = true;

        [ViewVariables]
        public ShuttleMode Mode { get; set; } = ShuttleMode.Cruise;
    }

    public enum ShuttleMode : byte
    {
        Docking,
        Cruise,
    }
}
