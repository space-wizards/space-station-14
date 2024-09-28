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

/// <summary>
/// A networked event raised by the server to request the current visual state of a target player entity
/// </summary>
[Serializable, NetSerializable]
public sealed class PlayerSpriteStateRequest : EntityEventArgs
{
    /// <summary>
    /// The player entity in question
    /// </summary>
    public readonly NetEntity TargetPlayer;

    public PlayerSpriteStateRequest(NetEntity targetPlayer)
    {
        TargetPlayer = targetPlayer;
    }
}

/// <summary>
/// The client's response to a <see cref="PlayerSpriteStateRequest"/>
/// </summary>
[Serializable, NetSerializable]
public sealed class PlayerSpriteStateMessage : EntityEventArgs
{
    public readonly NetEntity SpriteEntity;

    /// <summary>
    /// Data needed to reconstruct the player's sprite component layers
    /// </summary>
    public readonly SpriteLayerDatum[] SpriteLayerData;

    public PlayerSpriteStateMessage(NetEntity spriteEntity, SpriteLayerDatum[] spriteLayerData)
    {
        SpriteEntity = spriteEntity;
        SpriteLayerData = spriteLayerData;
    }
}

/// <summary>
/// Data for a single sprite component layer
/// </summary>
[Serializable, NetSerializable]
public sealed class SpriteLayerDatum
{
    public string RSIPath;
    public string RSIState;

    public SpriteLayerDatum(string rsiPath, string rsiState)
    {
        RSIPath = rsiPath;
        RSIState = rsiState;
    }
}
