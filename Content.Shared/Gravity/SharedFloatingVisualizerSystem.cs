using Robust.Shared.GameStates;

namespace Content.Shared.Gravity;

/// <summary>
/// Handles offsetting a sprite when there is no gravity
/// </summary>
public abstract class SharedFloatingVisualizerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedFloatingVisualsComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<GravityChangedEvent>(OnGravityChanged);
        SubscribeLocalEvent<SharedFloatingVisualsComponent, EntParentChangedMessage>(OnEntParentChanged);
        SubscribeLocalEvent<SharedFloatingVisualsComponent, ComponentGetState>(OnComponentGetState);
        SubscribeLocalEvent<SharedFloatingVisualsComponent, ComponentHandleState>(OnComponentHandleState);
    }

    /// <summary>
    /// Offsets a sprite with a linear interpolation animation
    /// </summary>
    public virtual void FloatAnimation(EntityUid uid, Vector2 offset, string animationKey, float animationTime, bool stop = false) { }

    protected bool HasGravity(EntityUid uid, SharedFloatingVisualsComponent component, EntityUid? gridUid = null)
    {
        var grid = gridUid ?? Transform(uid).GridUid;
        if (grid == null || !TryComp<GravityComponent>(grid, out var gravity) || !gravity.Enabled)
        {
            component.HasGravity = false;
            Dirty(component);
            return false;
        } else {
            component.HasGravity = true;
            Dirty(component);
            return true;
        }
    }

    private void OnComponentStartup(EntityUid uid, SharedFloatingVisualsComponent component, ComponentStartup args)
    {
        if (!HasGravity(uid, component))
            FloatAnimation(uid, component.Offset, component.AnimationKey, component.AnimationTime);
    }

    private void OnGravityChanged(ref GravityChangedEvent args)
    {
        foreach (var component in EntityQuery<SharedFloatingVisualsComponent>())
        {
            var uid = component.Owner;
            component.HasGravity = args.HasGravity;
            Dirty(component);

            if (!args.HasGravity)
                FloatAnimation(uid, component.Offset, component.AnimationKey, component.AnimationTime);
        }
    }

    private void OnEntParentChanged(EntityUid uid, SharedFloatingVisualsComponent component, ref EntParentChangedMessage args)
    {
        var gridUid = args.Transform.GridUid;
        if (!HasGravity(uid, component, gridUid))
            FloatAnimation(uid, component.Offset, component.AnimationKey, component.AnimationTime);
    }

    private void OnComponentGetState(EntityUid uid, SharedFloatingVisualsComponent component, ref ComponentGetState args)
    {
        args.State = new SharedFloatingVisualsComponentState(component.AnimationTime, component.Offset, component.HasGravity);
    }

    private void OnComponentHandleState(EntityUid uid, SharedFloatingVisualsComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not SharedFloatingVisualsComponentState state)
            return;

        component.AnimationTime = state.AnimationTime;
        component.Offset = state.Offset;
        component.HasGravity = state.HasGravity;
    }
}
