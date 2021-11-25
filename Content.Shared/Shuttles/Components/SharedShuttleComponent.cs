using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Shuttles.Components
{
    public abstract class SharedShuttleComponent : Component
    {
        public override string Name => "Shuttle";

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
