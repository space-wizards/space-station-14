using System.Diagnostics.CodeAnalysis;
using Content.Shared._Starlight.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.Map.Components;

namespace Content.Shared._Starlight.Eye.Blinding.Systems;

public sealed class ChildBlockVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;

    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<ChildBlockVisionComponent> _blockQuery;
    private EntityQuery<MapGridComponent> _mapQuery;
    public override void Initialize()
    {
        base.Initialize();

        _transformQuery = GetEntityQuery<TransformComponent>();
        _blockQuery = GetEntityQuery<ChildBlockVisionComponent>();
        _mapQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<ParentCanBlockVisionComponent, CanSeeAttemptEvent>(OnSeeAttempt);
        SubscribeLocalEvent<ParentCanBlockVisionComponent, EntParentChangedMessage>(OnChangeParent);
    }

    private void OnChangeParent(Entity<ParentCanBlockVisionComponent> ent, ref EntParentChangedMessage args) => _blindable.UpdateIsBlind(ent.Owner, true);

    private void OnSeeAttempt(Entity<ParentCanBlockVisionComponent> ent, ref CanSeeAttemptEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        var parent = _transform.GetParentUid(ent);
        if (parent.IsValid() && HaveBlockVisionParent(parent))
            args.Cancel();
    }

    /// <summary>
    /// recursively go through all parents up to the map. If one of the parents has a vision blocking component, we see nothing.
    /// </summary>
    private bool HaveBlockVisionParent(EntityUid ent)
    {
        if (!_transformQuery.TryComp(ent, out var xform))
            return false;

        if (_mapQuery.HasComp(ent))
            return false;

        if (_blockQuery.TryComp(ent, out var block) && block.Enabled)
            return true;

        return HaveBlockVisionParent(xform.ParentUid);
    }

    #region Parent Component

    /// <summary>
    /// Gets enabled state from <see cref="ChildBlockVisionComponent"/>. Returns true if successful.
    /// </summary>
    /// <param name="ent">entity from which we get enabled state.</param>
    /// <param name="Enabled">Out nullable boolean which determines enabled state of component.</param>
    /// <returns>boolean which determines if get successful.</returns>
    public bool TryGetChildBlockVision(Entity<ChildBlockVisionComponent?> ent, [NotNullWhen(true)] out bool? Enabled)
    {
        Enabled = null;
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        Enabled = ent.Comp.Enabled;
        return true;
    }

    /// <summary>
    /// Tries to set enabled state for <see cref="ChildBlockVisionComponent"/>. Returns true if successful.
    /// </summary>
    /// <param name="ent">entity for which we set enabled state.</param>
    /// <param name="enabled">boolean which we need to set. Can be nullable, if it is, just toggles enabled state.</param>
    /// <returns>boolean which determines if set successful.</returns>
    public bool TrySetChildBlockVision(Entity<ChildBlockVisionComponent?> ent, bool? enabled)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        enabled = enabled == null ? !ent.Comp.Enabled : enabled;

        SetChildBlockVision((ent, ent.Comp), enabled.Value);
        return true;
    }

    /// <summary>
    /// Sets enabled state for <see cref="ChildBlockVisionComponent"/>. Better to use <see cref="TrySetChildBlockVision"/>.
    /// </summary>
    /// <param name="ent">entity for which we set enabled state.</param>
    /// <param name="enabled">boolean which we need to set.</param>
    public void SetChildBlockVision(Entity<ChildBlockVisionComponent> ent, bool enabled)
    {
        ent.Comp.Enabled = enabled;
        Dirty(ent);
    }
    #endregion

    #region Child Component
    
    /// <summary>
    /// Gets enabled state from <see cref="ParentCanBlockVisionComponent"/>. Returns true if successful.
    /// </summary>
    /// <param name="ent">entity from which we get enabled state.</param>
    /// <param name="Enabled">Out nullable boolean which determines enabled state of component.</param>
    /// <returns>boolean which determines if get successful.</returns>
    public bool TryGetParentCanBlockVision(Entity<ParentCanBlockVisionComponent?> ent, [NotNullWhen(true)] out bool? Enabled)
    {
        Enabled = null;
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        Enabled = ent.Comp.Enabled;
        return true;
    }

    /// <summary>
    /// Tries to set enabled state for <see cref="ParentCanBlockVisionComponent"/>. Returns true if successful.
    /// </summary>
    /// <param name="ent">entity for which we set enabled state.</param>
    /// <param name="enabled">boolean which we need to set. Can be nullable, if it is, just toggles enabled state.</param>
    /// <returns>boolean which determines if set successful.</returns>
    public bool TrySetParentCanBlockVision(Entity<ParentCanBlockVisionComponent?> ent, bool? enabled)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        enabled = enabled == null ? !ent.Comp.Enabled : enabled;

        SetParentCanBlockVision((ent, ent.Comp), enabled.Value);
        return true;
    }

    /// <summary>
    /// Sets enabled state for <see cref="ParentCanBlockVisionComponent"/>. Better to use <see cref="TrySetParentCanBlockVision"/>.
    /// </summary>
    /// <param name="ent">entity for which we set enabled state.</param>
    /// <param name="enabled">boolean which we need to set.</param>
    public void SetParentCanBlockVision(Entity<ParentCanBlockVisionComponent> ent, bool enabled)
    {
        ent.Comp.Enabled = enabled;
        Dirty(ent);
    }
    #endregion
}