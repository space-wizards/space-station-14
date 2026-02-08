using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.EntitySystems;

// This part provides public functions for use in implementing commands.
public sealed partial class SatiationSystem
{
    /// <summary>
    /// Indexes and returns the <see cref="SatiationTypePrototype"/> for the given <paramref name="protoId"/>. If no
    /// such prototype exists, returns null.
    /// </summary>
    /// <remarks>
    /// It is expected that <paramref name="protoId"/> is a possibly-invalid user-provided string, so it's fine if it
    /// doesn't exist.
    /// </remarks>
    public SatiationTypePrototype? GetTypeOrNull(string protoId)
    {
        _prototype.TryIndex<SatiationTypePrototype>(protoId, out var proto);
        return proto;
    }

    /// <summary>
    /// Returns the all of the <see cref="SatiationPrototype.Keys">key strings</see> of the given
    /// <paramref name="type"/> for <paramref name="entity"/>, or empty if no such type exists.
    /// </summary>
    /// <remarks>
    /// It is expected that <paramref name="type"/> is validated with <see cref="GetTypeOrNull"/> before calling this.
    /// If it fails to resolve, an error will be logged.
    /// </remarks>
    public IEnumerable<string> GetKeysForType(
        Entity<SatiationComponent> entity,
        [ForbidLiteral] ProtoId<SatiationTypePrototype> type
    )
    {
        return GetAndResolveSatiationOfType(entity, type)?.Proto.AllThresholdKeys ?? [];
    }

    /// <summary>
    /// Returns the <see cref="SatiationPrototype.MaximumValue"/> of the given <paramref name="type"/> for
    /// <paramref name="entity"/>, or null if no such type exists.
    /// </summary>
    /// <remarks>
    /// It is expected that <paramref name="type"/> is validated with <see cref="GetTypeOrNull"/> before calling this.
    /// If it fails to resolve, an error will be logged.
    /// </remarks>
    public int? GetMaximumValue(
        Entity<SatiationComponent> entity,
        [ForbidLiteral] ProtoId<SatiationTypePrototype> type
    ) => GetAndResolveSatiationOfType(entity, type)?.Proto.MaximumValue;
}
