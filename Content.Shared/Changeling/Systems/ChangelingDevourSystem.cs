using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Armor;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Changeling.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Changeling.Systems;

public sealed class ChangelingDevourSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedChangelingIdentitySystem _changelingIdentitySystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingDevourComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourActionEvent>(OnDevourAction);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourWindupDoAfterEvent>(OnDevourWindup);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourConsumeDoAfterEvent>(OnDevourConsume);
        SubscribeLocalEvent<ChangelingDevourComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<ChangelingDevourComponent> ent, ref MapInitEvent args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.ChangelingDevourActionEntity, ent.Comp.ChangelingDevourAction);
    }

    private void OnShutdown(Entity<ChangelingDevourComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ChangelingDevourActionEntity != null)
        {
            _actionsSystem.RemoveAction(ent.Owner, ent.Comp.ChangelingDevourActionEntity);
        }
    }

    /// <summary>
    /// Checkes if the targets outerclothing is beyond a DamageCoefficientThreshold to protect them from being devoured.
    /// </summary>
    /// <param name="target">The Targeted entity</param>
    /// <param name="ent">Changelings Devour Component</param>
    /// <returns>Is the target Protected from the attack</returns>
    private bool IsTargetProtected(EntityUid target, Entity<ChangelingDevourComponent> ent)
    {
        var ev = new CoefficientQueryEvent(SlotFlags.OUTERCLOTHING);

        RaiseLocalEvent(target, ev);

        foreach (var compProtectiveDamageType in ent.Comp.ProtectiveDamageTypes)
        {
            if (!ev.DamageModifiers.Coefficients.TryGetValue(compProtectiveDamageType, out var coefficient))
                continue;
            if (coefficient < 1f - ent.Comp.DevourPreventionPercentageThreshold)
                return true;
        }

        return false;
    }

    // The action was used.
    // Start the first doafter for the windup.
    private void OnDevourAction(Entity<ChangelingDevourComponent> ent, ref ChangelingDevourActionEvent args)
    {
        if (args.Handled
            || _whitelistSystem.IsWhitelistFailOrNull(ent.Comp.Whitelist, args.Target)
            || !HasComp<ChangelingIdentityComponent>(ent))
            return;

        args.Handled = true;
        var target = args.Target;

        if (target == ent.Owner)
            return; // don't eat yourself

        if (!_mobState.IsDead(target))
        {
            _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-not-dead"), args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (HasComp<RottingComponent>(target))
        {
            _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-rotting"), args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (IsTargetProtected(target, ent))
        {
            _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-protected"), ent, ent, PopupType.Medium);
            return;
        }

        if (_net.IsServer)
        {
            ent.Comp.CurrentDevourSound = _audio.Stop(ent.Comp.CurrentDevourSound);
            var pvsSound = _audio.PlayPvs(ent.Comp.DevourWindupNoise, ent);
            if (pvsSound != null)
                ent.Comp.CurrentDevourSound = pvsSound.Value.Entity;
        }

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ent:player} started changeling devour windup against {target:player}");

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, ent.Comp.DevourWindupTime, new ChangelingDevourWindupDoAfterEvent(), ent, target: target, used: ent)
        {
            BreakOnMove = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.None,
        });

        var selfMessage = Loc.GetString("changeling-devour-begin-windup-self", ("user", Identity.Entity(ent.Owner, EntityManager)));
        var othersMessage = Loc.GetString("changeling-devour-begin-windup-others", ("user", Identity.Entity(ent.Owner, EntityManager)));
        _popupSystem.PopupPredicted(
            selfMessage,
            othersMessage,
            args.Performer,
            args.Performer,
            PopupType.MediumCaution);
    }

    // First doafter finished.
    // Start the second doafter for the actual consumption and deal a small amount of damage.
    private void OnDevourWindup(Entity<ChangelingDevourComponent> ent, ref ChangelingDevourWindupDoAfterEvent args)
    {
        args.Handled = true;
        ent.Comp.CurrentDevourSound = _audio.Stop(ent.Comp.CurrentDevourSound);

        if (args.Cancelled)
            return;

        if (args.Target is not { } target)
            return;

        _damageable.ChangeDamage(target, ent.Comp.WindupDamage, true, true, ent.Owner);

        var selfMessage = Loc.GetString("changeling-devour-begin-consume-self", ("user", Identity.Entity(ent.Owner, EntityManager)));
        var othersMessage = Loc.GetString("changeling-devour-begin-consume-others", ("user", Identity.Entity(ent.Owner, EntityManager)));
        _popupSystem.PopupPredicted(
            selfMessage,
            othersMessage,
            args.User,
            args.User,
            PopupType.LargeCaution);

        if (_net.IsServer)
        {
            var pvsSound = _audio.PlayPvs(ent.Comp.ConsumeNoise, ent);

            if (pvsSound != null)
                ent.Comp.CurrentDevourSound = pvsSound.Value.Entity;
        }

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player} began to devour {ToPrettyString(target):player}'s identity");

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            ent,
            ent.Comp.DevourConsumeTime,
            new ChangelingDevourConsumeDoAfterEvent(),
            ent,
            target: target,
            used: ent)
        {
            BreakOnMove = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.None,
        });
    }

    // Second doafter finished.
    // Save the identity and deal more damage.
    private void OnDevourConsume(Entity<ChangelingDevourComponent> ent, ref ChangelingDevourConsumeDoAfterEvent args)
    {
        args.Handled = true;
        ent.Comp.CurrentDevourSound = _audio.Stop(ent.Comp.CurrentDevourSound);

        if (args.Cancelled)
            return;

        if (args.Target is not { } target)
            return;

        _damageable.ChangeDamage(target, ent.Comp.DevourDamage, true, true, ent.Owner);

        if (!_mobState.IsDead(target))
        {
            _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-not-dead"), args.User, args.User, PopupType.Medium);
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player} unsuccessfully devoured {ToPrettyString(args.Target):player}'s identity");
            return;
        }

        var selfMessage = Loc.GetString("changeling-devour-consume-complete-self", ("user", Identity.Entity(args.User, EntityManager)));
        var othersMessage = Loc.GetString("changeling-devour-consume-complete-others", ("user", Identity.Entity(args.User, EntityManager)));
        _popupSystem.PopupPredicted(
            selfMessage,
            othersMessage,
            args.User,
            args.User,
            PopupType.LargeCaution);

        if (_mobState.IsDead(target)
            && HasComp<HumanoidProfileComponent>(target)
            && TryComp<ChangelingIdentityComponent>(args.User, out var identityStorage))
        {
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player} successfully devoured {ToPrettyString(target):player}'s identity");
            _changelingIdentitySystem.CloneToPausedMap((ent, identityStorage), target);

            if (_inventorySystem.TryGetSlotEntity(target, "jumpsuit", out var item)
                && TryComp<ButcherableComponent>(item, out var butcherable))
                RipClothing(target, (item.Value, butcherable));
        }

        Dirty(ent);
    }

    // TODO: This should just be an API method in the butcher system
    private void RipClothing(EntityUid victim, Entity<ButcherableComponent> item)
    {
        var spawnEntities = EntitySpawnCollection.GetSpawns(item.Comp.SpawnedEntities, _robustRandom);

        foreach (var proto in spawnEntities)
        {
            // TODO: once predictedRandom is in, make this a Coordinate offset of 0.25f from the victims position
            PredictedSpawnNextToOrDrop(proto, victim);
        }

        PredictedQueueDel(item.Owner);
    }
}
