using Content.Shared.Disease;
using Content.Shared.Damage;
using JetBrains.Annotations;

namespace Content.Server.Disease.Effects
{
    /// <summary>
    /// Deals or heals damage to the host
    /// </summary>
    [UsedImplicitly]
    public sealed class DiseaseHealthChange : DiseaseEffect
    {
        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;
        public override void Effect(DiseaseEffectArgs args)
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<DamageableSystem>().TryChangeDamage(args.DiseasedEntity, Damage, true, false);
        }
    }
}
