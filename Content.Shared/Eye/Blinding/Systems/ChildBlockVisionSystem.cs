using Content.Shared.Eye.Blinding.Components;
using Robust.Shared.Map.Components;

namespace Content.Shared.Eye.Blinding.Systems;

public sealed class ChildBlockVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;

    private EntityQuery<ChildBlockVisionComponent> _blockQuery;
    private EntityQuery<MapGridComponent> _mapQuery;
    public override void Initialize()
    {
        base.Initialize();

        _blockQuery = GetEntityQuery<ChildBlockVisionComponent>();
        _mapQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<EyeComponent, CanSeeAttemptEvent>(OnSeeAttempt);
        SubscribeLocalEvent<EyeComponent, EntParentChangedMessage>(OnChangeParent);
    }

    private void OnChangeParent(Entity<EyeComponent> ent, ref EntParentChangedMessage args)
    {
        _blindable.UpdateIsBlind(ent.Owner);
    }

    private void OnSeeAttempt(Entity<EyeComponent> ent, ref CanSeeAttemptEvent args)
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
        if (_mapQuery.HasComp(ent))
            return false;

        if (_blockQuery.TryComp(ent, out var block) && block.Enabled)
            return true;

        return HaveBlockVisionParent(_transform.GetParentUid(ent));
    }
}
