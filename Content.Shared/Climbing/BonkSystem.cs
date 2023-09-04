using Content.Shared.Interaction;
using Content.Shared.Stunnable;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Robust.Shared.Configuration;
using Content.Shared.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Components;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Climbing;

[InjectDependencies]
public sealed partial class BonkSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private DamageableSystem _damageableSystem = default!;
    [Dependency] private SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private SharedStunSystem _stunSystem = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BonkableComponent, DragDropTargetEvent>(OnDragDrop);
        SubscribeLocalEvent<BonkableComponent, BonkDoAfterEvent>(OnBonkDoAfter);
    }

    private void OnBonkDoAfter(EntityUid uid, BonkableComponent component, BonkDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        TryBonk(args.Args.User, uid, component);

        args.Handled = true;
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

    private void OnDragDrop(EntityUid uid, BonkableComponent component, ref DragDropTargetEvent args)
    {
        if (args.Handled || !HasComp<ClumsyComponent>(args.Dragged))
            return;

        var doAfterArgs = new DoAfterArgs(args.Dragged, component.BonkDelay, new BonkDoAfterEvent(), uid, target: uid)
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
