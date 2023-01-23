using Content.Server.Interaction;
using Content.Server.Stunnable;
using Content.Shared.CCVar;
using Content.Shared.Climbing;
using Content.Shared.Damage;
using Content.Shared.DragDrop;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;

namespace Content.Server.Climbing
{
    public sealed class BonkSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly InteractionSystem _interactionSystem = default!;
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BonkableComponent, DragDropEvent>(OnDragDrop);
        }

        public bool TryBonk(EntityUid user, BonkableComponent bonkComponent)
        {
            if (!_cfg.GetCVar(CCVars.GameTableBonk))
            {
                // Not set to always bonk, try clumsy roll.
                if (!_interactionSystem.TryRollClumsy(user, bonkComponent.BonkClumsyChance))
                    return false;
            }

            // BONK!

            _audioSystem.PlayPvs(bonkComponent.BonkSound, bonkComponent.Owner);
            _stunSystem.TryParalyze(user, TimeSpan.FromSeconds(bonkComponent.BonkTime), true);

            if (bonkComponent.BonkDamage is { } bonkDmg)
                _damageableSystem.TryChangeDamage(user, bonkDmg, true, origin: user);

            return true;
        }

        private void OnDragDrop(EntityUid user, BonkableComponent bonkComponent, DragDropEvent args)
        {
            TryBonk(args.Dragged, bonkComponent);
        }
    }
}
