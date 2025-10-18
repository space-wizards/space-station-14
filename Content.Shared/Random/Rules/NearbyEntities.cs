using Content.Shared.Whitelist;

namespace Content.Shared.Random.Rules;

/// <summary>
/// Checks for entities matching the whitelist in range.
/// This is more expensive than <see cref="NearbyComponentsRule"/> so prefer that!
/// </summary>
public sealed partial class NearbyEntitiesRule : RulesRule
{
    /// <summary>
    /// How many of the entity need to be nearby.
    /// </summary>
    [DataField]
    public int Count = 1;

    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    [DataField]
    public float Range = 10f;

    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        if (!entManager.TryGetComponent(uid, out TransformComponent? xform) ||
            xform.MapUid == null)
        {
            return false;
        }

        var transform = entManager.System<SharedTransformSystem>();
        var lookup = entManager.System<EntityLookupSystem>();
        var whitelistSystem = entManager.System<EntityWhitelistSystem>();

        var found = false;
        var worldPos = transform.GetWorldPosition(xform);
        var count = 0;

        foreach (var ent in lookup.GetEntitiesInRange(xform.MapID, worldPos, Range))
        {
            if (whitelistSystem.IsWhitelistFail(Whitelist, ent))
                continue;

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
