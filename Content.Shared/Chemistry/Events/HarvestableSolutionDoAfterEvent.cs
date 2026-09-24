using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Events;

/// <summary>
/// Do-after event for harvesting a solution.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class HarvestableSolutionDoAfterEvent : SimpleDoAfterEvent;
