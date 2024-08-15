using Robust.Shared.Prototypes;

namespace Content.Shared.Random.Rules;

public sealed partial class NearbyComponentsRule : RulesRule
{
    /// <summary>
    /// Does the entity need to be anchored.
    /// </summary>
    [DataField]
    public bool Anchored;

    [DataField]
    public int Count;

    [DataField(required: true)]
    public ComponentRegistry Components = default!;

    [DataField]
    public float Range = 10f;

    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        var inRange = new HashSet<Entity<IComponent>>();
        var xformQuery = entManager.GetEntityQuery<TransformComponent>();

        if (!xformQuery.TryGetComponent(uid, out var xform) ||
            xform.MapUid == null)
        {
            return false;
        }

        var transform = entManager.System<SharedTransformSystem>();
        var lookup = entManager.System<EntityLookupSystem>();

        var found = false;
        var worldPos = transform.GetWorldPosition(xform);
        var count = 0;

        foreach (var compType in Components.Values)
        {
            inRange.Clear();
            lookup.GetEntitiesInRange(compType.Component.GetType(), xform.MapID, worldPos, Range, inRange);
            foreach (var comp in inRange)
            {
                if (Anchored &&
                    (!xformQuery.TryGetComponent(comp, out var compXform) ||
                     !compXform.Anchored))
                {
                    continue;
                }

                count++;

                if (count < Count)
                    continue;

                found = true;
                break;
            }

            if (found)
                break;
        }

        if (!found)
            return Inverted;

        return !Inverted;
    }
}
