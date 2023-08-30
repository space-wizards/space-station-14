using Content.Shared.Construction.Components;
using Robust.Shared.Map.Components;

namespace Content.Shared.Construction.EntitySystems;

public sealed partial class AnchorableSystem
{
    private void InitializeUniqueDirection()
    {
        SubscribeLocalEvent<AnchorUniqueDirectionComponent, AnchorAttemptEvent>(OnAnchorAttempt);
    }

    private void OnAnchorAttempt(EntityUid uid, AnchorUniqueDirectionComponent component, AnchorAttemptEvent args)
    {
        var grid = Comp<MapGridComponent>(args.GridUid);
        var directions = component.Directions;
        directions = args.LocalRotation.RotateDir(directions.AsDir()).AsFlag();
        var anchored = grid.GetAnchoredEntitiesEnumerator(args.GridIndex);

        while (anchored.MoveNext(out var anchoredEnt))
        {
            if (!_anchoredUniqueQuery.TryGetComponent(anchoredEnt, out var otherUnique) ||
                !_xformQuery.TryGetComponent(anchoredEnt, out var otherXform))
            {
                continue;
            }

            var otherDirections = otherXform.LocalRotation.RotateDir(otherUnique.Directions.AsDir()).AsFlag();

            if ((directions & otherDirections) != 0x0)
            {
                args.Cancel();
                return;
            }
        }
    }
}
