using Content.Shared.CCVar;
using Content.Shared.Climbing.Components;
using Content.Shared.Climbing.Events;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
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
        SubscribeLocalEvent<BonkableComponent, BonkDoAfterEvent>(OnBonkDoAfter);
        SubscribeLocalEvent<BonkableComponent, AttemptClimbEvent>(OnAttemptClimb);
    }

    private void OnBonkDoAfter(EntityUid uid, BonkableComponent component, BonkDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Used == null)
            return;

        TryBonk(args.Args.Used.Value, uid, component, source: args.Args.User);

        args.Handled = true;
    }


    public bool TryBonk(EntityUid user, EntityUid bonkableUid, BonkableComponent? bonkableComponent = null, EntityUid? source = null)
    {
        if (!Resolve(bonkableUid, ref bonkableComponent, false))
            return false;

        // BONK!
        var userName = Identity.Entity(user, EntityManager);
        var bonkableName = Identity.Entity(bonkableUid, EntityManager);

        if (user == source)
        {
            // Non-local, non-bonking players
            var othersMessage = Loc.GetString("bonkable-success-message-others", ("user", userName), ("bonkable", bonkableName));
            // Local, bonking player
            var selfMessage = Loc.GetString("bonkable-success-message-user", ("user", userName), ("bonkable", bonkableName));

            _popupSystem.PopupPredicted(selfMessage, othersMessage, user, user);
        }
        else if (source != null)
        {
            // Local, non-bonking player (dragger)
            _popupSystem.PopupClient(Loc.GetString("bonkable-success-message-others", ("user", userName), ("bonkable", bonkableName)), user, source.Value);
            // Non-local, non-bonking players
            _popupSystem.PopupEntity(Loc.GetString("bonkable-success-message-others", ("user", userName), ("bonkable", bonkableName)), user, Filter.Pvs(user).RemoveWhereAttachedEntity(e => e == user || e == source.Value), true);
            // Non-local, bonking player
            _popupSystem.PopupEntity(Loc.GetString("bonkable-success-message-user", ("user", userName), ("bonkable", bonkableName)), user, user);
        }



        if (source != null)
            _audioSystem.PlayPredicted(bonkableComponent.BonkSound, bonkableUid, source);
        else
            _audioSystem.PlayPvs(bonkableComponent.BonkSound, bonkableUid);

        _stunSystem.TryParalyze(user, TimeSpan.FromSeconds(bonkableComponent.BonkTime), true);

        if (bonkableComponent.BonkDamage is { } bonkDmg)
            _damageableSystem.TryChangeDamage(user, bonkDmg, true, origin: user);

        return true;

    }

    private bool TryStartBonk(EntityUid uid, EntityUid user, EntityUid climber, BonkableComponent? bonkableComponent = null)
    {
        if (!Resolve(uid, ref bonkableComponent, false))
            return false;

        if (!HasComp<ClumsyComponent>(climber) || !HasComp<HandsComponent>(user))
            return false;

        if (!_cfg.GetCVar(CCVars.GameTableBonk))
        {
            // Not set to always bonk, try clumsy roll.
            if (!_interactionSystem.TryRollClumsy(climber, bonkableComponent.BonkClumsyChance))
                return false;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, user, bonkableComponent.BonkDelay, new BonkDoAfterEvent(), uid, target: uid, used: climber)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            DuplicateCondition = DuplicateConditions.SameTool | DuplicateConditions.SameTarget
        };

        return _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnAttemptClimb(EntityUid uid, BonkableComponent component, ref AttemptClimbEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryStartBonk(uid, args.User, args.Climber, component))
            args.Cancelled = true;
    }

    [Serializable, NetSerializable]
    private sealed partial class BonkDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
