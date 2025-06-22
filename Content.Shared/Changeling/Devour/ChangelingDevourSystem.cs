using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Armor;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Changeling.Devour;

public sealed class ChangelingDevourSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly ChangelingIdentitySystem _changelingIdentitySystem = default!;
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
        SubscribeLocalEvent<ChangelingDevourComponent, DoAfterAttemptEvent<ChangelingDevourConsumeDoAfterEvent>>(OnConsumeAttemptTick);
    }

    private void OnMapInit(Entity<ChangelingDevourComponent> ent, ref MapInitEvent args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.ChangelingDevourActionEntity, ent.Comp.ChangelingDevourAction);
    }

    private void OnConsumeAttemptTick(Entity<ChangelingDevourComponent> ent,
       ref DoAfterAttemptEvent<ChangelingDevourConsumeDoAfterEvent> eventData)
    {
        var curTime = _timing.CurTime;

        if (curTime < ent.Comp.NextTick)
            return;

        ConsumeDamageTick(eventData.Event.Target, ent.Comp, eventData.Event.User);

        ent.Comp.NextTick += ent.Comp.DamageTimeBetweenTicks; //TODO: Add this to the component
    }

    private void ConsumeDamageTick(EntityUid? target, ChangelingDevourComponent comp, EntityUid? user)
    {
        if (target == null)
            return;

        if (!TryComp<DamageableComponent>(target, out var damage))
            return;

        foreach (var damagePoints in comp.DamagePerTick.DamageDict)
        {

            if (damage.Damage.DamageDict.TryGetValue(damagePoints.Key, out var val) && val > comp.DevourConsumeDamageCap)
                return;
        }
        _damageable.TryChangeDamage(target, comp.DamagePerTick, true, true, damage, user);

    }

    private bool TargetIsProtected(EntityUid target, Entity<ChangelingDevourComponent> ent)
    {
        var ev = new CoefficientQueryEvent(SlotFlags.OUTERCLOTHING);

        RaiseLocalEvent(target, ev, true);

        foreach (var compProtectiveDamageType in ent.Comp.ProtectiveDamageTypes)
        {
            if (!ev.DamageModifiers.Coefficients.TryGetValue(compProtectiveDamageType, out var coefficient))
                continue;
            if (coefficient < 1f - ent.Comp.DevourPreventionPercentageThreshold)
                return true;
        }

        return false;
    }

    private void OnDevourAction(Entity<ChangelingDevourComponent> ent, ref ChangelingDevourActionEvent args)
    {

        if (args.Handled || _whitelistSystem.IsWhitelistFailOrNull(ent.Comp.Whitelist, args.Target)
                         || !HasComp<ChangelingIdentityComponent>(ent))
            return;

        args.Handled = true;
        var target = args.Target;

        if(target == ent.Owner)
            return; // don't eat yourself

        if (HasComp<RottingComponent>(target))
        {
            _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-rotting"), args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (TargetIsProtected(target, ent))
        {
            _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-protected"), ent, ent, PopupType.Medium);
            return;
        }

        if (HasComp<ChangelingHuskedCorpseComponent>(target))
        {
            _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-husked"), args.Performer, args.Performer);
            return;
        }

        if (_net.IsServer)
            ent.Comp.CurrentDevourSound = _audio.PlayPvs(ent.Comp.DevourWindupNoise!, ent,  new AudioParams())!.Value.Entity;

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ent:player} started changeling devour windup against {target:player}");

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, ent.Comp.DevourWindupTime, new ChangelingDevourWindupDoAfterEvent(), ent, target: target, used: ent)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.None,
        });

        _popupSystem.PopupPredicted(Loc.GetString("changeling-devour-begin-windup"), args.Performer, null, PopupType.MediumCaution);

    }

    private void OnDevourWindup(Entity<ChangelingDevourComponent> ent, ref ChangelingDevourWindupDoAfterEvent args)
    {
        var curTime = _timing.CurTime;
        args.Handled = true;

        _audio.Stop(ent.Comp.CurrentDevourSound!);

        if (args.Cancelled)
            return;

        _popupSystem.PopupPredicted(Loc.GetString("changeling-devour-begin-consume"),
            args.User,
            null,
            PopupType.LargeCaution);

        if (_net.IsServer)
            ent.Comp.CurrentDevourSound = _audio.PlayPvs(ent.Comp.ConsumeNoise!, ent,  new AudioParams())!.Value.Entity;

        ent.Comp.NextTick = curTime + ent.Comp.DamageTimeBetweenTicks;

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player} began to devour {ToPrettyString(args.Target):player} identity");

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            ent,
            ent.Comp.DevourConsumeTime,
            new ChangelingDevourConsumeDoAfterEvent(),
            ent,
            target: args.Target,
            used: ent)
        {
            AttemptFrequency = AttemptFrequency.EveryTick,
            BreakOnMove = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.None,
        });
    }

    private void OnDevourConsume(Entity<ChangelingDevourComponent> ent, ref ChangelingDevourConsumeDoAfterEvent args)
    {
        args.Handled = true;
        var target = args.Target;

        if (target == null)
            return;

        _audio.Stop(ent.Comp.CurrentDevourSound!);

        if (args.Cancelled)
            return;

        if (!_mobState.IsDead((EntityUid)target))
        {
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player}  unsuccessfully devoured {ToPrettyString(args.Target):player}'s identity");
            _popupSystem.PopupClient(Loc.GetString("changeling-devour-consume-failed-not-dead"), args.User,  args.User, PopupType.Medium);
            return;
        }

        _popupSystem.PopupPredicted(Loc.GetString("changeling-devour-consume-complete"), args.User, null, PopupType.LargeCaution);

        if (_mobState.IsDead(target.Value)
            && TryComp<BodyComponent>(target, out var body)
            && HasComp<HumanoidAppearanceComponent>(target)
            && TryComp<ChangelingIdentityComponent>(args.User, out var identityStorage))
        {
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player}  successfully devoured {ToPrettyString(args.Target):player}'s identity");
            _changelingIdentitySystem.CloneToNullspace((ent, identityStorage), target.Value);

            if (ent.Comp.Husking)
            {
                EnsureComp<ChangelingHuskedCorpseComponent>(target.Value);
            }


            foreach (var organ in _bodySystem.GetBodyOrgans(target, body))
            {
                _entityManager.QueueDeleteEntity(organ.Id);
            }

            if (_inventorySystem.TryGetSlotEntity(target.Value, "jumpsuit", out var item)
                && TryComp<ButcherableComponent>(item, out var butcherable))
                RipClothing(target.Value, (item.Value, butcherable));
        }
        Dirty(ent);
    }
    private void RipClothing(EntityUid victim, Entity<ButcherableComponent> item)
    {
        var spawnEntities = EntitySpawnCollection.GetSpawns(item.Comp.SpawnedEntities, _robustRandom);

        foreach (var proto in spawnEntities)
        {
            // TODO: once predictedRandom is in, make this a Coordinate offset of 0.25f from the victims position
            PredictedSpawnNextToOrDrop(proto, victim);
        }

        QueueDel(item);
    }
}
