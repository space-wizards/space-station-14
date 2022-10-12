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
        SubscribeLocalEvent<StealthComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<StealthComponent, EntityPausedEvent>(OnPaused);
        SubscribeLocalEvent<StealthComponent, ComponentInit>(OnInit);
    }

    private void OnPaused(EntityUid uid, StealthComponent component, EntityPausedEvent args)
    {
        if (args.Paused)
        {
            component.LastVisibility = GetVisibility(uid, component);
            component.LastUpdated = null;
        }
        else
        {
            component.LastUpdated = _timing.CurTime;
        }

        Dirty(component);
    }

    protected virtual void OnInit(EntityUid uid, StealthComponent component, ComponentInit args)
    {
        if (component.LastUpdated != null || Paused(uid))
            return;

        component.LastUpdated = _timing.CurTime;
    }

    private void OnStealthGetState(EntityUid uid, StealthComponent component, ref ComponentGetState args)
    {
        args.State = new StealthComponentState(component.LastVisibility, component.LastUpdated);
    }

    private void OnStealthHandleState(EntityUid uid, StealthComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not StealthComponentState cast)
            return;

        component.LastVisibility = cast.Visibility;
        component.LastUpdated = cast.LastUpdated;
    }

    private void OnMove(EntityUid uid, StealthComponent component, ref MoveEvent args)
    {
        if (args.FromStateHandling)
            return;

        if (args.NewPosition.EntityId != args.OldPosition.EntityId)
            return;

        var delta = component.MovementVisibilityRate * (args.NewPosition.Position - args.OldPosition.Position).Length;
        ModifyVisibility(uid, delta, component);
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

        component.LastVisibility = Math.Clamp(component.LastVisibility + delta, -1f, 1f);
        Dirty(component);
    }

    /// <summary>
    /// Sets the visibility directly with no modifications
    /// </summary>
    /// <param name="value">The value to set the visibility to. -1 is fully invisible, 1 is fully visible</param>
    public void SetVisibility(EntityUid uid, float value, StealthComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.LastVisibility = value;
        if (component.LastUpdated != null)
            component.LastUpdated = _timing.CurTime;

        Dirty(component);
    }

    /// <summary>
    /// Gets the current visibility from the <see cref="StealthComponent"/>
    /// Use this instead of getting LastVisibility from the component directly.
    /// </summary>
    /// <returns>Returns a calculation that accounts for any stealth change that happened since last update, otherwise returns based on if it can resolve the component.</returns>
    public float GetVisibility(EntityUid uid, StealthComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return 1;

        if (component.LastUpdated == null)
            return component.LastVisibility;

        var deltaTime = _timing.CurTime - component.LastUpdated.Value;
        return Math.Clamp(component.LastVisibility + (float) deltaTime.TotalSeconds * component.PassiveVisibilityRate, -1f, 1f);
    }
}
