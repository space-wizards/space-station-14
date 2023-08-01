using Content.Server.Chainsaw.Systems;
using Content.Shared.Damage;
using Content.Shared.Timing;
using Robust.Shared.Audio;

namespace Content.Server.Chainsaw.Components
{
    [RegisterComponent, Access(typeof(ChainsawSystem))]
    public sealed class ChainsawComponent : Component
    {
        public bool Activated = false;

        [DataField("hitSound")]
        public SoundSpecifier HitSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/chainsaw_hit.ogg");

        [DataField("activeSound")]
        public SoundSpecifier ActiveSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/chainsaw_start.ogg");

        [DataField("onDamage")]
        public DamageSpecifier OnDamage = new();
    }
}
