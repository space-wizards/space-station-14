using Content.Shared.Interaction;
using Content.Shared.Stunnable;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.DragDrop;
using Robust.Shared.Configuration;
using Content.Shared.Popups;
using Content.Shared.IdentityManagement;
using Robust.Shared.Player;

namespace Content.Shared.Climbing;

public sealed class BonkSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BonkableComponent, DragDropTargetEvent>(OnDragDrop);
    }

    public bool TryBonk(EntityUid user, EntityUid bonkableUid, BonkableComponent? bonkableComponent = null)
    {
        if (!Resolve(bonkableUid, ref bonkableComponent, false))
            return false;

        if (!_cfg.GetCVar(CCVars.GameTableBonk))
        {
            // Not set to always bonk, try clumsy roll.
            if (!_interactionSystem.TryRollClumsy(user, bonkableComponent.BonkClumsyChance))
                return false;
        }

        // BONK!
        var userName = Identity.Entity(user, EntityManager);
        var bonkableName = Identity.Entity(bonkableUid, EntityManager);

        _popupSystem.PopupEntity(Loc.GetString("bonkable-success-message-others", ("user", userName), ("bonkable", bonkableName)), user, Filter.PvsExcept(user), true);

        _popupSystem.PopupEntity(Loc.GetString("bonkable-success-message-user", ("user", userName), ("bonkable", bonkableName)), user, user);

        _audioSystem.PlayPvs(bonkableComponent.BonkSound, bonkableUid);
        _stunSystem.TryParalyze(user, TimeSpan.FromSeconds(bonkableComponent.BonkTime), true);

        if (bonkableComponent.BonkDamage is { } bonkDmg)
            _damageableSystem.TryChangeDamage(user, bonkDmg, true, origin: user);

        return true;

    }

    private void OnDragDrop(EntityUid uid, BonkableComponent bonkableComponent, ref DragDropTargetEvent args)
    {
        TryBonk(args.Dragged, uid);
    }
}
