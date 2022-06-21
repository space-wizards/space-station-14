using Content.Server.Weapon.Melee;
using Content.Server.Stunnable;
using Content.Shared.StatusEffect;

namespace Content.Server.Weapon.StunOnHit
{
    public sealed class StunOnHitSystem : EntitySystem
    {
        [Dependency] private readonly StunSystem _stunSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StunOnHitComponent, MeleeHitEvent>(OnMeleeHit);
        }

        private void OnMeleeHit(EntityUid uid, StunOnHitComponent component, MeleeHitEvent args)
        {
            if (component.Disabled)
                return;

            foreach (var entity in args.HitEntities)
            {
                if (!TryComp<StatusEffectsComponent>(entity, out var status))
                    continue;

                _stunSystem.TryParalyze(entity, TimeSpan.FromSeconds(component.ParalyzeTime), true, status);
            }
        }
    }
}
