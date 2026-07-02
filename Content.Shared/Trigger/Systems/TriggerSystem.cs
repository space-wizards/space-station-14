using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.EntityTable;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Timing;
using Content.Shared.Trigger.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using Robust.Shared.Audio.Systems;


namespace Content.Shared.Trigger.Systems;

/// <summary>
/// System containing the basic trigger API.
/// </summary>
/// <remarks>
/// If you add a custom trigger subscription or effect then don't put them here.
/// Put them into a separate system so we don't end up with a giant list of imports.
/// </remarks>
public sealed partial class TriggerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private FixtureSystem _fixture = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private ItemToggleSystem _itemToggle = default!;
    [Dependency] private SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;

    public const string DefaultTriggerKey = "trigger";

    public override void Initialize()
    {
        base.Initialize();

        InitializeCollide();
        InitializeCondition();
        InitializeInteraction();
        InitializeProximity();
        InitializeSignal();
        InitializeTimer();
        InitializeSpawn();
        InitializeVoice();
    }

    /// <summary>
    /// Trigger the given entity.
    /// </summary>
    /// <param name="trigger">The entity that has the components that should be triggered.</param>
    /// <param name="user">The user of the trigger. Some effects may target the user instead of the trigger entity.</param>
    /// <param name="key">A key string to allow multiple, independent triggers on the same entity. If null then all triggers will activate.</param>
    /// <param name="predicted">Whether or not this trigger is being predicted</param>
    /// <returns>Whether or not the trigger has sucessfully activated an effect.</returns>
    public bool Trigger(EntityUid trigger, EntityUid? user = null, string? key = null, bool predicted = true)
    {
        var attemptTriggerEvent = new AttemptTriggerEvent(user, key);
        RaiseLocalEvent(trigger, ref attemptTriggerEvent);
        if (attemptTriggerEvent.Cancelled)
            return false;

        var triggerEvent = new TriggerEvent(user, key, predicted);
        RaiseLocalEvent(trigger, ref triggerEvent, true);
        return triggerEvent.Handled;
    }

    /// <summary>
    /// Activate a timer trigger on an entity with <see cref="TimerTriggerComponent"/>.
    /// </summary>
    /// <returns>Whether or not a timer was activated.</returns>
    public bool ActivateTimerTrigger(Entity<TimerTriggerComponent?> ent, EntityUid? user = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (Terminating(ent))
            return false; // Stop trying to resurrect a dead horse.

        if (HasComp<ActiveTimerTriggerComponent>(ent))
            return false; // already activated

        if (user != null)
        {
            _adminLogger.Add(LogType.Trigger,
                $"{ToPrettyString(user.Value):user} started a {ent.Comp.Delay} second timer trigger on entity {ToPrettyString(ent.Owner):timer}");
        }
        else
        {
            _adminLogger.Add(LogType.Trigger,
                $"{ent.Comp.Delay} second timer trigger started on entity {ToPrettyString(ent.Owner):timer}");
        }

        if (ent.Comp.Popup != null)
            _popup.PopupPredicted(Loc.GetString(ent.Comp.Popup.Value, ("device", ent.Owner)), ent.Owner, user);

        AddComp<ActiveTimerTriggerComponent>(ent);
        var curTime = _timing.CurTime;
        ent.Comp.NextTrigger = curTime + ent.Comp.Delay;
        var delay = ent.Comp.InitialBeepDelay ?? ent.Comp.BeepInterval;
        ent.Comp.NextBeep = curTime + delay;
        ent.Comp.User = user;
        Dirty(ent);

        var ev = new ActiveTimerTriggerEvent(user);
        RaiseLocalEvent(ent.Owner, ref ev);

        if (TryComp<AppearanceComponent>(ent, out var appearance))
            _appearance.SetData(ent.Owner, TriggerVisuals.VisualState, TriggerVisualState.Primed, appearance);

        return true;
    }

    /// <summary>
    /// Stop a timer trigger on an entity with <see cref="TimerTriggerComponent"/>.
    /// </summary>
    /// <returns>Whether or not a timer was stopped.</returns>
    public bool StopTimerTrigger(Entity<TimerTriggerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!HasComp<ActiveTimerTriggerComponent>(ent))
            return false; // the timer is not active

        RemComp<ActiveTimerTriggerComponent>(ent);
        if (TryComp<AppearanceComponent>(ent.Owner, out var appearance))
            _appearance.SetData(ent.Owner, TriggerVisuals.VisualState, TriggerVisualState.Unprimed, appearance);

        _adminLogger.Add(LogType.Trigger, $"A timer trigger was stopped before triggering on entity {ToPrettyString(ent.Owner):timer}");
        return true;
    }

    /// <summary>
    /// Delay an active timer trigger.
    /// Returns false if not active.
    /// </summary>
    /// <param name="amount">The time to add.</param>
    public bool TryDelay(Entity<TimerTriggerComponent?> ent, TimeSpan amount)
    {
        if (!Resolve(ent, ref ent.Comp, false) || !HasComp<ActiveTimerTriggerComponent>(ent))
            return false;

        ent.Comp.NextTrigger += amount;
        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Setter for the Delay datafield.
    /// </summary>
    public void SetDelay(Entity<TimerTriggerComponent?> ent, TimeSpan delay)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Delay = delay;
        Dirty(ent);
    }

    /// <summary>
    /// Gets the remaining time until the trigger will activate.
    /// Returns null if the trigger is not currently active.
    /// </summary>
    public TimeSpan? GetRemainingTime(Entity<TimerTriggerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false) || !HasComp<ActiveTimerTriggerComponent>(ent))
            return null; // not a timer or not currently active

        return ent.Comp.NextTrigger - _timing.CurTime;
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateTimer();
        UpdateRepeat();
        UpdateProximity();
        UpdateTimedCollide();
    }
}
