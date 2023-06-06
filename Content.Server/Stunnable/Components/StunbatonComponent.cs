using Content.Server.Stunnable.Systems;
using Content.Shared.Timing;
using Robust.Shared.Audio;

namespace Content.Server.Stunnable.Components
{
    [RegisterComponent, Access(typeof(StunbatonSystem))]
    public sealed class StunbatonComponent : Component
    {
        public bool Activated = false;
        public const string SolutionName = "baton";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("rigged")]
        public bool IsRigged = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("energyPerUse")]
        public float EnergyPerUse { get; set; } = 350;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("onThrowStunChance")]
        public float OnThrowStunChance { get; set; } = 0.20f;

        [DataField("stunSound")]
        public SoundSpecifier StunSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/egloves.ogg");

        [DataField("sparksSound")]
        public SoundSpecifier SparksSound { get; set; } = new SoundCollectionSpecifier("sparks");

        [DataField("turnOnFailSound")]
        public SoundSpecifier TurnOnFailSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/button.ogg");
    }
}
