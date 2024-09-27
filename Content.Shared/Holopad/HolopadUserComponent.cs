using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Holopad;

/// <summary>
/// Holds data pertaining to entities that are using holopads
/// </summary>
/// <remarks>
/// This component is added and removed automatically from entities
/// </remarks>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedHolopadSystem))]
public sealed partial class HolopadUserComponent : Component
{
    /// <summary>
    /// A list of holopads that the user is interacting with
    /// </summary>
    [ViewVariables]
    public HashSet<Entity<HolopadComponent>> LinkedHolopads = new();
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
    public readonly NetEntity? Target;

    public HolopadHologramVisualsUpdateEvent(NetEntity hologram, NetEntity? target = null)
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
