using Content.Shared.Traitor.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Traitor;

public sealed class StructureHackedEvent : EntityEventArgs;

public sealed class StructureHackCompletedEvent : EntityEventArgs;

/// <summary>
///     Event raised when the hijack beacon succeeds in hijacking the ATS.
/// </summary>
[ByRefEvent]
public record struct HijackBeaconSuccessEvent(int Fine)
{
    public int Total = 0;
};
