namespace Content.Shared.Random;

public sealed class RulesSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public bool IsTrue(EntityUid uid, RulesPrototype rules)
    {
        foreach (var rule in rules.Rules)
        {
            switch (rule)
            {
                case AlwaysTrueRule:
                    break;
                case InSpaceRule:
                {
                    if (!TryComp<TransformComponent>(uid, out var xform) ||
                        xform.GridUid != null)
                    {
                        return false;
                    }

                    break;
                }
                case NearbyComponentsRule nearbyComps:
                {
                    if (!TryComp<TransformComponent>(uid, out var xform) ||
                        xform.MapUid == null)
                    {
                        return false;
                    }

                    var found = false;
                    var worldPos = _transform.GetWorldPosition(xform);

                    foreach (var comp in nearbyComps.Components.Values)
                    {
                        if (!_lookup.AnyComponentsInRange(comp.Component.GetType(), xform.MapID, worldPos, nearbyComps.Range))
                            continue;

                        found = true;
                        break;
                    }

                    if (!found)
                        return false;

                    break;
                }
                case OnMapGridRule:
                {
                    if (!TryComp<TransformComponent>(uid, out var xform) ||
                        xform.GridUid != xform.MapUid ||
                        xform.MapUid == null)
                    {
                        return false;
                    }

                    break;
                }
                default:
                    throw new NotImplementedException();
            }
        }

        return true;
    }
}
