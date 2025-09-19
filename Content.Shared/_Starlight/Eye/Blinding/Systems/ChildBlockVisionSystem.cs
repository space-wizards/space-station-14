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

    private void OnChangeParent(Entity<ParentCanBlockVisionComponent> ent, ref EntParentChangedMessage args)
    {
        _blindable.UpdateIsBlind(ent.Owner);
    }

    private void OnSeeAttempt(Entity<ParentCanBlockVisionComponent> ent, ref CanSeeAttemptEvent args)
    {
        var parent = _transform.GetParentUid(ent);
        if (HaveBlockVisionParent(parent))
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
}