using Content.Shared.Actions;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Changeling.Devour;

public abstract partial class SharedChangelingDevourSystem : EntitySystem
{

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedChangelingIdentitySystem _changelingIdentitySystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingDevourComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourActionEvent>(OnDevourAction);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourWindupDoAfterEvent>(OnDevourWindup);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourConsumeDoAfterEvent>(OnDevourConsume);
        SubscribeLocalEvent<ChangelingDevourComponent, DoAfterAttemptEvent<ChangelingDevourConsumeDoAfterEvent>>(OnConsumeAttemptTick);
    }
    private void OnInit(EntityUid uid, ChangelingDevourComponent component, MapInitEvent args)
    {
        if(!component.ChangelingDevourActionEntity.HasValue)
            _actionsSystem.AddAction(uid, ref component.ChangelingDevourActionEntity, component.ChangelingDevourAction);

        var identityStorage = EnsureComp<ChangelingIdentityComponent>(uid);

        _changelingIdentitySystem.CloneLingStart(uid, identityStorage); // Clone yourself so you can transform back.
    }

    private void OnConsumeAttemptTick(EntityUid uid,
        ChangelingDevourComponent component,
        DoAfterAttemptEvent<ChangelingDevourConsumeDoAfterEvent> eventData)
    {
        var curTime = _timing.CurTime;

        if (curTime < component.NextTick)
            return;

        ConsumeDamageTick(eventData.Event.Target, component, eventData.Event.User);

        component.NextTick += TimeSpan.FromSeconds(1f);
    }

    private void ConsumeDamageTick(EntityUid? target, ChangelingDevourComponent comp, EntityUid? user)
    {
        if (target == null)
            return;

        if (!TryComp<DamageableComponent>(target, out var damage))
            return;

        if (damage.DamagePerGroup.TryGetValue("Brute", out var val) && val < comp.DevourConsumeDamageCap)
        {
            _damageable.TryChangeDamage(target, comp.DamagePerTick, true, true, damage, user);
        }
    }


    private void OnDevourAction(EntityUid uid, ChangelingDevourComponent component, ChangelingDevourActionEvent args)
    {

        if (args.Handled || _whitelistSystem.IsWhitelistFailOrNull(component.Whitelist, args.Target)
                         || !TryComp<ChangelingIdentityComponent>(uid, out var identityStorage)
                         || !HasComp<DamageableComponent>(args.Target))
            return;

        args.Handled = true;
        var target = args.Target;

        if(target == uid)
            return; // don't eat yourself

        if (HasComp<RottingComponent>(target))
        {
            _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-rotting"), args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        var ev = new ChangelingDevourAttemptEvent(component.DevourPreventionPercentageThreshold, SlotFlags.OUTERCLOTHING); // Check the Targets outerclothes for Mitigation coefficents

        RaiseLocalEvent(target, ev, true);

        if (ev.Protection)
        {
            _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-protected"), uid, uid, PopupType.Medium);
            return;
        }

        if (HasComp<ChangelingHuskedCorpseComponent>(target))
        {
            _popupSystem.PopupClient(Loc.GetString("changeling-devour-attempt-failed-husk"), args.Performer, args.Performer);
            return;
        }
        StartSound(uid, component, new SoundPathSpecifier(
            _audio.GetSound(component.DevourWindupNoise!)));

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.DevourWindupTime, new ChangelingDevourWindupDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.None,
        });

        _popupSystem.PopupPredicted(Loc.GetString("changeling-devour-begin-windup"), args.Performer, null, PopupType.MediumCaution);

    }
    private void OnDevourWindup(EntityUid uid, ChangelingDevourComponent component, ChangelingDevourWindupDoAfterEvent args)
    {
        var curTime = _timing.CurTime;
        args.Handled = true;

        StopSound(uid, component);

        if (args.Cancelled)
            return;

        _popupSystem.PopupPredicted(Loc.GetString("changeling-devour-begin-consume"),
            args.User,
            null,
            PopupType.LargeCaution);

        StartSound(uid, component, new SoundPathSpecifier(_audio.GetSound(component.ConsumeNoise!)));

        component.NextTick = curTime + TimeSpan.FromSeconds(1);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            uid,
            component.DevourConsumeTime,
            new ChangelingDevourConsumeDoAfterEvent(),
            uid,
            target: args.Target,
            used: uid)
        {
            AttemptFrequency = AttemptFrequency.EveryTick,
            BreakOnMove = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.None,
        });
    }
    private void OnDevourConsume(EntityUid uid, ChangelingDevourComponent component, ChangelingDevourConsumeDoAfterEvent args)
    {
        args.Handled = true;
        var target = args.Target;

        if (target == null)
            return;

        StopSound(uid, component);

        if (args.Cancelled)
            return;

        if (!_mobState.IsDead((EntityUid)target))
        {
            _popupSystem.PopupClient(Loc.GetString("changeling-devour-consume-failed-not-dead"), args.User,  args.User, PopupType.Medium);
            return;
        }

        _popupSystem.PopupPredicted(Loc.GetString("changeling-devour-consume-complete"), args.User, null, PopupType.LargeCaution);

        if (_mobState.IsDead(target.Value)
            && TryComp<BodyComponent>(target, out var body)
            && HasComp<HumanoidAppearanceComponent>(target)
            && TryComp<ChangelingIdentityComponent>(args.User, out var identityStorage))
        {
            _changelingIdentitySystem.CloneToNullspace(uid, identityStorage, target.Value);
            EnsureComp<ChangelingHuskedCorpseComponent>(target.Value);

            foreach (var organ in _bodySystem.GetBodyOrgans(target, body))
            {
                _entityManager.QueueDeleteEntity(organ.Id);
            }

            if (_inventorySystem.TryGetSlotEntity(target.Value, "jumpsuit", out var item)
                && TryComp<ButcherableComponent>(item, out var butcherable))
            {
                RipClothing(target.Value, item.Value, butcherable);
            }
        }
        Dirty(uid, component);
    }

    protected virtual void StartSound(EntityUid uid, ChangelingDevourComponent component, SoundSpecifier? sound){ }
    protected virtual void StopSound(EntityUid uid, ChangelingDevourComponent component) { }

    protected virtual void RipClothing(EntityUid uid, EntityUid item, ButcherableComponent butcherable) { }

}
/// <summary>
/// Raised to check if Changelings devour attempt should be blocked, based on if the value is over the protectionThreshold
/// </summary>
public sealed class ChangelingDevourAttemptEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; }

    public readonly double ProtectionThreshold;
    public bool Protection;

    public ChangelingDevourAttemptEvent(double protectionThreshold, SlotFlags slots = ~SlotFlags.POCKET)
    {
        TargetSlots = slots;
        ProtectionThreshold = protectionThreshold;
    }

}

public sealed partial class ChangelingDevourActionEvent : EntityTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ChangelingDevourWindupDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ChangelingDevourConsumeDoAfterEvent : SimpleDoAfterEvent
{
}



