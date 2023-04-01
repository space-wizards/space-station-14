using Content.Shared._Afterlight.ThirdDimension;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._Afterlight.ThirdDimension;

/// <summary>
/// This handles...
/// </summary>
public sealed class LadderSystem : EntitySystem
{
    [Dependency] private readonly SharedZLevelSystem _zLevel = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<LadderComponent, InteractHandEvent>(OnInteractHand);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<LadderComponent, TransformComponent>();

        while (query.MoveNext(out _, out var ladder, out var xform))
        {
            if (!ladder.Primary || Deleted(ladder.OtherHalf))
                return;

            // Track it, it "hangs" from above.
            _transform.SetWorldPosition(ladder.OtherHalf.Value, _transform.GetWorldPosition(xform));
        }
    }

    private void OnInteractHand(EntityUid uid, LadderComponent component, InteractHandEvent args)
    {
        EnsureOpposing(uid, component);
        _zLevel.TryTraverse(!component.Primary, args.User);
    }

    private void EnsureOpposing(EntityUid uid, LadderComponent ladder)
    {
        if (!ladder.Primary || !Deleted(ladder.OtherHalf))
            return;

        var parentXform = Transform(uid);
        var maybeBelow = _zLevel.MapBelow[(int) parentXform.MapID];

        if (maybeBelow is not {} below)
            return;

        var newCoords = new MapCoordinates(_transform.GetWorldPosition(parentXform), below);
        ladder.OtherHalf = Spawn(ladder.OtherHalfProto, newCoords);
    }
}
