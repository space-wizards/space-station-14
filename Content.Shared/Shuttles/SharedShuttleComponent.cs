using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Shuttles
{
    public abstract class SharedShuttleComponent : Component
    {
        public override string Name => "Shuttle";

        [ViewVariables]
        public virtual bool Enabled { get; set; } = true;

        /// <summary>
        /// How much our linear impulse is multiplied by.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float SpeedMultipler { get; set; } = 200.0f;

        [ViewVariables]
        public ShuttleMode Mode { get; set; } = ShuttleMode.Docking;
    }

    public enum ShuttleMode : byte
    {
        Docking,
        Cruise,
    }
}
