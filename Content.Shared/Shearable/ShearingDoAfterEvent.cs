using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Shearing;

/// <summary>
///     Thrown whenever an animal is sheared.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ShearingDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
///     Thrown whenever an animal's shearable sprite layer updates.
///     Which is either on a change of mobstate or a change of shearable solution.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ShearableLayerUpdateEvent : EntityEventArgs
{
    /// <summary>
    /// The entity associated with the event.
    /// </summary>
    public EntityUid Uid { get; init; }

    /// <summary>
    /// Whether to enable or disable the visibility layer.
    /// If null then unchanged. 
    /// </summary>
    public bool? ToggleVisibility = null;

    /// <summary>
    /// If passed, contains details about the mob's life state..
    /// </summary>
    public MobStateComponent? MobState = null;
}

