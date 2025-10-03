using Content.Shared.Eye;

namespace Content.Shared.Ghost;

/// <summary>
/// System for the <see cref="GhostComponent"/>.
/// Prevents ghosts from interacting when <see cref="GhostComponent.CanGhostInteract"/> is false.
/// </summary>
public abstract class SharedGhostVisibilitySystem : EntitySystem
{
    public bool AllVisible { get; protected set; }

    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedVisibilitySystem _visibility = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostVisibilityComponent, ComponentStartup>(OnGhostVisStartup);
        SubscribeLocalEvent<GhostVisibilityComponent, ComponentShutdown>(OnGhostVisShutdown);
        SubscribeLocalEvent<GhostVisibilityComponent, GetVisMaskEvent>(OnGhostVis);
        SubscribeLocalEvent<ToggleGhostVisibilityToAllEvent>(OnToggleGhostVisibilityToAll);
    }

    private void OnGhostVisStartup(EntityUid uid, GhostVisibilityComponent component, ComponentStartup args)
    {
        EnsureComp<VisibilityComponent>(uid);
        UpdateVisibility((uid, component));
        _eye.RefreshVisibilityMask(uid);
    }

    private void OnGhostVisShutdown(EntityUid uid, GhostVisibilityComponent component, ComponentShutdown args)
    {
        UpdateVisibility((uid, component));
        _eye.RefreshVisibilityMask(uid);
    }

    private void OnGhostVis(Entity<GhostVisibilityComponent> ent, ref GetVisMaskEvent args)
    {
        // If component not deleting they can see ghosts.
        if (ent.Comp.LifeStage <= ComponentLifeStage.Running)
            args.VisibilityMask |= (int)ent.Comp.Mask;
    }

    protected virtual void UpdateVisibility(Entity<GhostVisibilityComponent?, VisibilityComponent?> ent)
    {
        if (Terminating(ent.Owner))
            return;

        if (!Resolve(ent.Owner, ref ent.Comp1))
            return;

        SetVisible(ent, ShouldBeVisible(ent.Comp1));
    }

    protected virtual void SetVisible(Entity<GhostVisibilityComponent?, VisibilityComponent?> ghost, bool visible)
    {
        if (!Resolve(ghost.Owner, ref ghost.Comp1))
            return;

        if (ghost.Comp1.Visible == visible && ghost.Comp1.LifeStage >= ComponentLifeStage.Running)
            return;

        // VisibilityComponent might not exist yet, and will not get added on client
        Resolve(ghost.Owner, ref ghost.Comp2, false);

        if (visible)
        {
            _visibility.RemoveLayer((ghost.Owner, ghost.Comp2), (ushort)ghost.Comp1.Layer, false);
            _visibility.AddLayer((ghost.Owner, ghost.Comp2), (ushort)VisibilityFlags.Normal, false);
        }
        else
        {
            _visibility.AddLayer((ghost.Owner, ghost.Comp2), (ushort)ghost.Comp1.Layer, false);
            _visibility.RemoveLayer((ghost.Owner, ghost.Comp2), (ushort)VisibilityFlags.Normal, false);
        }

        _visibility.RefreshVisibility(ghost.Owner, ghost.Comp2);

        ghost.Comp1.Visible = visible;
        Dirty(ghost.Owner, ghost.Comp1);
    }

    /// <summary>
    /// Make a ghost visible regardless of the usual ghost visibility rules.
    /// </summary>
    public void SetVisibleOverride(Entity<GhostVisibilityComponent?, VisibilityComponent?> ghost, bool? visOverride)
    {
        if (!Resolve(ghost.Owner, ref ghost.Comp1))
            return;

        if (ghost.Comp1.VisibleOverride == visOverride)
            return;

        ghost.Comp1.VisibleOverride = visOverride;
        UpdateVisibility(ghost);
    }

    protected virtual bool ShouldBeVisible(GhostVisibilityComponent comp)
    {
        if (comp.VisibleOverride is {} val)
            return val;

        return AllVisible && !comp.IgnoreGlobalVisibility;
    }

    /// <summary>
    /// Set visibility of all whitelisted "observer" ghosts.
    /// </summary>
    public void SetAllVisible(bool visible)
    {
        if (AllVisible == visible)
            return;

        AllVisible = visible;

        var entityQuery = EntityQueryEnumerator<GhostVisibilityComponent, VisibilityComponent>();
        while (entityQuery.MoveNext(out var uid, out var ghost, out var vis))
        {
            if (!ghost.IgnoreGlobalVisibility)
                UpdateVisibility((uid, ghost, vis));
        }
    }

    /// Handle wizard ghost visibility action
    private void OnToggleGhostVisibilityToAll(ToggleGhostVisibilityToAllEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = true;

        // TODO make this actually toggle?
        SetAllVisible(true);
    }

}
