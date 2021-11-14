using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent]
    public sealed class ThrusterComponent : Component
    {
        public override string Name => "Thruster";

        [ViewVariables]
        [DataField("enabled")]
        public bool Enabled = true;

        [ViewVariables]
        [DataField("impulse")]
        public float Impulse = 20f;

        [ViewVariables]
        [DataField("type")]
        public ThrusterType Type = ThrusterType.Linear;
    }

    public enum ThrusterType
    {
        Invalid = 0,
        Linear = 1 << 0,
        // Angular meaning rotational.
        Angular = 1 << 1,
    }
}
