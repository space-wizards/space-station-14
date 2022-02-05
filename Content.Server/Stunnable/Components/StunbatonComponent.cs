using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Stunnable.Components
{
    [RegisterComponent]
    public class StunbatonComponent : Component
    {
        public bool Activated = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeChanceNoSlowdown")]
        public float ParalyzeChanceNoSlowdown { get; set; } = 0.35f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeChanceWithSlowdown")]
        public float ParalyzeChanceWithSlowdown { get; set; } = 0.85f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeTime")]
        public float ParalyzeTime { get; set; } = 10f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("slowdownTime")]
        public float SlowdownTime { get; set; } = 5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("energyPerUse")]
        public float EnergyPerUse { get; set; } = 50;

        [DataField("stunSound")]
        public SoundSpecifier StunSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/egloves.ogg");

        [DataField("sparksSound")]
        public SoundSpecifier SparksSound { get; set; } = new SoundCollectionSpecifier("sparks");

        [DataField("turnOnFailSound")]
        public SoundSpecifier TurnOnFailSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/button.ogg");
    }
}
