using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.Medical;

/// <summary>
/// Raised on an entity to get its palpation descriptions.
/// </summary>
/// <param name="Messages">The list of descriptions to report.</param>
[ByRefEvent]
public readonly record struct PalpationEvent(List<string> Messages);

[Serializable, NetSerializable]
public sealed partial class PalpationDoAfterEvent : SimpleDoAfterEvent;
