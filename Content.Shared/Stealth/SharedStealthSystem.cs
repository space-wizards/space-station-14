using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stealth.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Stealth;

public abstract class SharedStealthSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StealthComponent, ComponentGetState>(OnStealthGetState);
        SubscribeLocalEvent<StealthComponent, ComponentHandleState>(OnStealthHandleState);
        SubscribeLocalEvent<StealthOnMoveComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<StealthOnMoveComponent, GetVisibilityModifiersEvent>(OnGetVisibilityModifiers);
        SubscribeLocalEvent<StealthComponent, EntityPausedEvent>(OnPaused);
        SubscribeLocalEvent<StealthComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<StealthComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<StealthComponent, ExamineAttemptEvent>(OnExamineAttempt);
        SubscribeLocalEvent<StealthComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<StealthComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnExamineAttempt(EntityUid uid, StealthComponent component, ExamineAttemptEvent args)
    {
        if (!component.Enabled || GetVisibility(uid, component) > component.ExamineThreshold)
            return;

        // Don't block examine for owner or children of the cloaked entity.
        // Containers and the like should already block examining, so not bothering to check for occluding containers.
        var source = args.Examiner;
        do
        {
            if (source == uid)
                return;
            source = Transform(source).ParentUid;
        }
        while (source.IsValid());

        args.Cancel();
    }

    private void OnExamined(EntityUid uid, StealthComponent component, ExaminedEvent args)
    {
        if (component.Enabled)
            args.PushMarkup(Loc.GetString(component.ExaminedDesc, ("target", uid)));
    }

    public virtual void SetEnabled(EntityUid uid, bool value, StealthComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) || component.Enabled == value)
            return;

        component.Enabled = value;
        Dirty(uid, component);
    }

    private void OnMobStateChanged(EntityUid uid, StealthComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            component.Enabled = component.EnabledOnDeath;
        }
        else
        {
            component.Enabled = true;
        }

        Dirty(uid, component);
    }

    private void OnPaused(EntityUid uid, StealthComponent component, ref EntityPausedEvent args)
    {
        component.LastVisibility = GetVisibility(uid, component);
        component.LastUpdated = null;
        Dirty(uid, component);
    }

    private void OnUnpaused(EntityUid uid, StealthComponent component, ref EntityUnpausedEvent args)
    {
        component.LastUpdated = _timing.CurTime;
        Dirty(uid, component);
    }

    protected virtual void OnInit(EntityUid uid, StealthComponent component, ComponentInit args)
    {
        if (component.LastUpdated != null || Paused(uid))
            return;

        component.LastUpdated = _timing.CurTime;
    }

    private void OnStealthGetState(EntityUid uid, StealthComponent component, ref ComponentGetState args)
    {
        args.State = new StealthComponentState(component.LastVisibility, component.LastUpdated, component.Enabled);
    }

    private void OnStealthHandleState(EntityUid uid, StealthComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not StealthComponentState cast)
            return;

        SetEnabled(uid, cast.Enabled, component);
        component.LastVisibility = cast.Visibility;
        component.LastUpdated = cast.LastUpdated;
    }

    private void OnMove(EntityUid uid, StealthOnMoveComponent component, ref MoveEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.NewPosition.EntityId != args.OldPosition.EntityId)
            return;

        var delta = component.MovementVisibilityRate * (args.NewPosition.Position - args.OldPosition.Position).Length();
        ModifyVisibility(uid, delta);
    }

    private void OnGetVisibilityModifiers(EntityUid uid, StealthOnMoveComponent component, GetVisibilityModifiersEvent args)
    {
        var mod = args.SecondsSinceUpdate * component.PassiveVisibilityRate;
        args.FlatModifier += mod;
    }

    /// <summary>
    /// Modifies the visibility based on the delta provided.
    /// </summary>
    /// <param name="delta">The delta to be used in visibility calculation.</param>
    public void ModifyVisibility(EntityUid uid, float delta, StealthComponent? component = null)
    {
        if (delta == 0 || !Resolve(uid, ref component))
            return;

        if (component.LastUpdated != null)
        {
            component.LastVisibility = GetVisibility(uid, component);
            component.LastUpdated = _timing.CurTime;
        }

        component.LastVisibility = Math.Clamp(component.LastVisibility + delta, component.MinVisibility, component.MaxVisibility);
        Dirty(uid, component);
    }

    /// <summary>
    /// Sets the visibility directly with no modifications
    /// </summary>
    /// <param name="value">The value to set the visibility to. -1 is fully invisible, 1 is fully visible</param>
    public void SetVisibility(EntityUid uid, float value, StealthComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.LastVisibility = Math.Clamp(value, component.MinVisibility, component.MaxVisibility);
        if (component.LastUpdated != null)
            component.LastUpdated = _timing.CurTime;

        Dirty(uid, component);
    }

    /// <summary>
    /// Gets the current visibility from the <see cref="StealthComponent"/>
    /// Use this instead of getting LastVisibility from the component directly.
    /// </summary>
    /// <returns>Returns a calculation that accounts for any stealth change that happened since last update, otherwise
    /// returns based on if it can resolve the component. Note that the returned value may be larger than the components
    /// maximum stealth value if it is currently disabled.</returns>
    public float GetVisibility(EntityUid uid, StealthComponent? component = null)
    {
        if (!Resolve(uid, ref component) || !component.Enabled)
            return 1;

        if (component.LastUpdated == null)
            return component.LastVisibility;

        var deltaTime = _timing.CurTime - component.LastUpdated.Value;

        var ev = new GetVisibilityModifiersEvent(uid, component, (float) deltaTime.TotalSeconds, 0f);
        RaiseLocalEvent(uid, ev, false);

        return Math.Clamp(component.LastVisibility + ev.FlatModifier, component.MinVisibility, component.MaxVisibility);
    }

    /// <summary>
    ///     Used to run through any stealth effecting components on the entity.
    /// </summary>
    private sealed class GetVisibilityModifiersEvent : EntityEventArgs
    {
        public readonly StealthComponent Stealth;
        public readonly float SecondsSinceUpdate;

        /// <summary>
        ///     Calculate this and add to it. Do not divide, multiply, or overwrite.
        ///     The sum will be added to the stealth component's visibility.
        /// </summary>
        public float FlatModifier;

        public GetVisibilityModifiersEvent(EntityUid uid, StealthComponent stealth, float secondsSinceUpdate, float flatModifier)
        {
            Stealth = stealth;
            SecondsSinceUpdate = secondsSinceUpdate;
            FlatModifier = flatModifier;
        }
    }
}
