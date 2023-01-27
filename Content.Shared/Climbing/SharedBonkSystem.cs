using Content.Shared.Interaction;
using Content.Shared.Stunnable;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.DragDrop;
using Robust.Shared.Configuration;
using Content.Shared.Popups;

namespace Content.Shared.Climbing;

public sealed class SharedBonkSystem : EntitySystem
{
    private const string BonkMessage = "Bonk!";

    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BonkableComponent, DragDropEvent>(OnDragDrop);
    }

    public bool TryBonk(EntityUid user, EntityUid climbable)
    {
        BonkableComponent? bonkableComponent = null;
        if (Resolve(climbable, ref bonkableComponent))
        {
            if (!_cfg.GetCVar(CCVars.GameTableBonk))
            {
                // Not set to always bonk, try clumsy roll.
                if (!_interactionSystem.TryRollClumsy(user, bonkableComponent.BonkClumsyChance))
                    return false;
            }

            // BONK!

            _popupSystem.PopupEntity(BonkMessage, user, PopupType.Medium);
            _audioSystem.PlayPvs(bonkableComponent.BonkSound, bonkableComponent.Owner);
            _stunSystem.TryParalyze(user, TimeSpan.FromSeconds(bonkableComponent.BonkTime), true);

            if (bonkableComponent.BonkDamage is { } bonkDmg)
                _damageableSystem.TryChangeDamage(user, bonkDmg, true, origin: user);

            return true;
        }

        return false;
    }

    private void OnDragDrop(EntityUid user, BonkableComponent bonkableComponent, DragDropEvent args)
    {
        TryBonk(args.Dragged, args.Target);
    }
}
