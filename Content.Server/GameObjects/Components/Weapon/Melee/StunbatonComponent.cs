#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class StunbatonComponent : Component
    {
        public override string Name => "Stunbaton";

        public bool Activated = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeChanceNoSlowdown")]
        public float ParalyzeChanceNoSlowdown => 0.35f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeChanceWithSlowdown")]
        public float ParalyzeChanceWithSlowdown => 0.85f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeTime")]
        public float ParalyzeTime => 10f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("slowdownTime")]
        public float SlowdownTime => 5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("energyPerUse")]
        public float EnergyPerUse => 50;
    }
}
