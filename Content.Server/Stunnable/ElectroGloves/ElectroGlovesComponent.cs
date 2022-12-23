using Content.Server.Stunnable.ElectroGloves;
using Content.Shared.Timing;
using Robust.Shared.Audio;

namespace Content.Server.Stunnable.ElectroGloves
{
    [RegisterComponent, Access(typeof(ElectroGlovesSystem))]
    public sealed class ElectroGlovesComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("energyPerUse")]
        public float EnergyPerUse { get; set; } = 100;

        [DataField("stunSound")]
        public SoundSpecifier StunSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/egloves.ogg");
    }
}
