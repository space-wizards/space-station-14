using Content.Shared.Chameleon.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Chameleon;

public abstract class SharedChameleonSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedChameleonComponent, ComponentGetState>(OnChameleonGetState);
        SubscribeLocalEvent<SharedChameleonComponent, ComponentHandleState>(OnChameleonHandlesState);
        SubscribeLocalEvent<SharedChameleonComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<SharedChameleonComponent, EntityPausedEvent>(OnPaused);
        SubscribeLocalEvent<SharedChameleonComponent, ComponentInit>(OnInit);
    }

    private void OnPaused(EntityUid uid, SharedChameleonComponent component, EntityPausedEvent args)
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

    protected virtual void OnInit(EntityUid uid, SharedChameleonComponent component, ComponentInit args)
    {
        if (component.LastUpdated != null || Paused(uid))
            return;

        component.LastUpdated = _timing.CurTime;
    }

    private void OnChameleonGetState(EntityUid uid, SharedChameleonComponent component, ref ComponentGetState args)
    {
        args.State = new ChameleonComponentState(component.LastVisibility, component.LastUpdated);
    }

    private void OnChameleonHandlesState(EntityUid uid, SharedChameleonComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ChameleonComponentState cast)
            return;

        component.LastVisibility = cast.Visibility;
        component.LastUpdated = cast.LastUpdated;
    }

    private void OnMove(EntityUid uid, SharedChameleonComponent component, ref MoveEvent args)
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
    public void ModifyVisibility(EntityUid uid, float delta, SharedChameleonComponent? component = null)
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
    public void SetVisibility(EntityUid uid, float value, SharedChameleonComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.LastVisibility = value;
        if (component.LastUpdated != null)
            component.LastUpdated = _timing.CurTime;

        Dirty(component);
    }

    /// <summary>
    /// Gets the current visibility from the <see cref="SharedChameleonComponent"/>
    /// Use this instead of getting LastVisibility from the component directly.
    /// </summary>
    /// <returns>Returns a calculation that accounts for any stealth change that happened since last update, otherwise returns based on if it can resolve the component.</returns>
    public float GetVisibility(EntityUid uid, SharedChameleonComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return 1;

        if (component.LastUpdated == null)
            return component.LastVisibility;

        var deltaTime = _timing.CurTime - component.LastUpdated.Value;
        return Math.Clamp(component.LastVisibility + (float) deltaTime.TotalSeconds * component.PassiveVisibilityRate, -1f, 1f);
    }
}
