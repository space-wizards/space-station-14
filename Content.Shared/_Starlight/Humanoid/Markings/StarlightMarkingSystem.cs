using System.Diagnostics.CodeAnalysis;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Humanoid.Markings;

public sealed class StarlightMarkingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public bool TryGetWaggingId(ProtoId<MarkingPrototype> markingId, [NotNullWhen(true)] out string? waggingId)
    {
        waggingId = null!;
        if (!_prototype.TryIndex(markingId, out var marking) ||
            marking.WaggingId == null)
        {
            return false;
        }

        waggingId = marking.WaggingId;
        return true;
    }
}
