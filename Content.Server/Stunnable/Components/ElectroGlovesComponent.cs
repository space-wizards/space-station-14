using Content.Server.Stunnable.Systems;
using Content.Shared.Timing;
using Robust.Shared.Audio;

namespace Content.Server.Stunnable.Components
{
    [RegisterComponent, Access(typeof(ElectroGlovesSystem))]
    public sealed class ElectroGlovesComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("energyPerUse")]
        public float EnergyPerUse { get; set; } = 350;

        [DataField("stunSound")]
        public SoundSpecifier StunSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/egloves.ogg");

        [DataField("sparksSound")]
        public SoundSpecifier SparksSound { get; set; } = new SoundCollectionSpecifier("sparks");
    }
}
