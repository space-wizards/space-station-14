using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Random.Rules;

/// <summary>
/// Checks for an entity nearby with the specified access.
/// </summary>
public sealed partial class NearbyAccessRule : RulesRule
{
    // This exists because of door electronics contained inside doors.
    /// <summary>
    /// Does the access entity need to be anchored.
    /// </summary>
    [DataField]
    public bool Anchored = true;

    /// <summary>
    /// Count of entities that need to be nearby.
    /// </summary>
    [DataField]
    public int Count = 1;

    [DataField(required: true)]
    public List<ProtoId<AccessLevelPrototype>> Access = new();

    [DataField]
    public float Range = 10f;

    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        var xformQuery = entManager.GetEntityQuery<TransformComponent>();

        if (!xformQuery.TryGetComponent(uid, out var xform) ||
            xform.MapUid == null)
        {
            return false;
        }

        var transform = entManager.System<SharedTransformSystem>();
        var lookup = entManager.System<EntityLookupSystem>();
        var reader = entManager.System<AccessReaderSystem>();

        var found = false;
        var worldPos = transform.GetWorldPosition(xform, xformQuery);
        var count = 0;

        // TODO: Update this when we get the callback version
        var entities = new HashSet<Entity<AccessReaderComponent>>();
        lookup.GetEntitiesInRange(xform.MapID, worldPos, Range, entities);
        foreach (var comp in entities)
        {
            if (!reader.AreAccessTagsAllowed(Access, comp) ||
                Anchored &&
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

        if (!found)
            return Inverted;

        return !Inverted;
    }
}
