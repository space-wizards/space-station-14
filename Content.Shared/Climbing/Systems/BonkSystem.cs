using Content.Shared.CCVar;
using Content.Shared.Climbing.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Climbing.Systems;

public sealed partial class BonkSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BonkableComponent, DragDropTargetEvent>(OnDragDrop);
        SubscribeLocalEvent<BonkableComponent, BonkDoAfterEvent>(OnBonkDoAfter);
    }

    private void OnBonkDoAfter(EntityUid uid, Components.BonkableComponent component, BonkDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null || args.Args.Target != args.Args.User)
            return;

        TryBonk(args.Args.User, uid, component);

        args.Handled = true;
    }


    public bool TryBonk(EntityUid user, EntityUid bonkableUid, Components.BonkableComponent? bonkableComponent = null)
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

    private void OnDragDrop(EntityUid uid, Components.BonkableComponent component, ref DragDropTargetEvent args)
    {
        if (args.Handled || !HasComp<ClumsyComponent>(args.Dragged) || !HasComp<HandsComponent>(args.User))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.Dragged, component.BonkDelay, new BonkDoAfterEvent(), uid, target: uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);

        args.Handled = true;
    }

    [Serializable, NetSerializable]
    private sealed partial class BonkDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
