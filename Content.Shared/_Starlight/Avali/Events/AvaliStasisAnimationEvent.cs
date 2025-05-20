using Robust.Shared.Serialization;
using Robust.Shared.Map;

namespace Content.Shared.Starlight.Avali.Events;

/// <summary>
/// The type of stasis animation to play
/// </summary>
[Serializable, NetSerializable]
public enum AvaliStasisAnimationType
{
    /// <summary>
    /// Animation played when preparing stasis
    /// </summary>
    Prepare,
    
    /// <summary>
    /// Animation played when entering stasis
    /// </summary>
    Enter,
    
    /// <summary>
    /// Animation played when exiting stasis
    /// </summary>
    Exit
}

/// <summary>
/// Network event for playing the stasis animation on all clients
/// </summary>
[Serializable, NetSerializable]
public sealed class AvaliStasisAnimationEvent : EntityEventArgs
{
    public NetEntity Entity;
    public NetCoordinates Coordinates;
    public AvaliStasisAnimationType AnimationType;

    public AvaliStasisAnimationEvent(NetEntity entity, NetCoordinates coordinates, AvaliStasisAnimationType animationType)
    {
        Entity = entity;
        Coordinates = coordinates;
        AnimationType = animationType;
    }
} 