using Content.Server.Damage.Systems;
using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Server.Damage.Components
{
    [Access(typeof(DamageOtherOnHitSystem))]
    [RegisterComponent]
    public sealed class DamageOtherOnHitComponent : Component
    {
        [DataField("ignoreResistances")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool IgnoreResistances = false;

        [DataField("stopOnHit")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool StopOnHit = true;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [DataField("soundHit")]
        [ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier? HitSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/genhit1.ogg");
    }
}
