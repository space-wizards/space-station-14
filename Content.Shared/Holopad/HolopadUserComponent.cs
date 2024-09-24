using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Holopad;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedHolopadSystem))]
public sealed partial class HolopadUserComponent : Component
{
    [ViewVariables]
    public EntityUid? LinkedHolopad = null;
}

/// <summary>
/// A networked event raised when the visual state of a hologram is being updated
/// </summary>
[Serializable, NetSerializable]
public sealed class HolopadHologramVisualsUpdateEvent : EntityEventArgs
{
    /// <summary>
    /// The hologram being updated
    /// </summary>
    public readonly NetEntity Hologram;

    /// <summary>
    /// The target the hologram is copying
    /// </summary>
    public readonly NetEntity Target;

    public HolopadHologramVisualsUpdateEvent(NetEntity hologram, NetEntity target)
    {
        Hologram = hologram;
        Target = target;
    }
}

/// <summary>
/// A networked event raised when the visual state of a hologram is being updated
/// </summary>
[Serializable, NetSerializable]
public sealed class HolopadUserTypingChangedEvent : EntityEventArgs
{
    /// <summary>
    /// The hologram being updated
    /// </summary>
    public readonly NetEntity User;

    /// <summary>
    /// The typing indicator state
    /// </summary>
    public readonly bool IsTyping;

    public HolopadUserTypingChangedEvent(NetEntity user, bool isTyping)
    {
        User = user;
        IsTyping = isTyping;
    }
}

[Serializable, NetSerializable]
public sealed class HolopadUserAppearanceChangedEvent : EntityEventArgs { }
