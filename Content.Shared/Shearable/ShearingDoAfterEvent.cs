using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Shearing;

/// <summary>
///     Thrown whenever an animal is sheared.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ShearingDoAfterEvent : SimpleDoAfterEvent { }
