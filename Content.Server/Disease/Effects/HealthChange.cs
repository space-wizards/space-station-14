using Content.Shared.Disease;
using Content.Shared.Damage;
using JetBrains.Annotations;

namespace Content.Server.Disease.Effects
{
    [UsedImplicitly]
    public sealed class HealthChange : DiseaseEffect
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;

        [DataField("damage", required: true)]
        public DamageSpecifier Damage = default!;

        public override void Effect(DiseaseEffectArgs args)
        {
            _damageableSystem.TryChangeDamage(args.DiseasedEntity, Damage);
        }

    }
}
