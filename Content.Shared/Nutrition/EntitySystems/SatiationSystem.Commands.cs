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
        ProtoMan.TryIndex<SatiationTypePrototype>(protoId, out var proto);
        return proto;
    }
}
