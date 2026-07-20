using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadmanSwitch;

/// <summary>
/// Raised when a user finishes toggling the deadman's switch in their hands.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class DeadmanSwitchDoAfterEvent : SimpleDoAfterEvent;
