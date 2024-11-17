using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Flip;

/// <summary>
/// Makes it possible for an entity to do a flip animation.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FlipAnimationComponent : Component
{
    /// <summary>
    /// What's the name of this animation? Make sure it's unique so it can play along side other animations.
    /// This prevents someone accidentally causing two identical effects to play on someone at the same time.
    /// </summary>
    [DataField]
    public string KeyName = "FlipEmote";

    /// <summary>
    /// How long should a complete flip take?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AnimationLength = 0.5f;
}

/// <summary>
/// Declares that an entity has started to flip.
/// </summary>
/// <param name="entity">The entity flipping.</param>
[Serializable, NetSerializable]
public sealed class StartFlipEvent(NetEntity entity) : EntityEventArgs
{
    public NetEntity Entity = entity;
}

/// <summary>
/// Declares that an entity has cancelled flipping.
/// </summary>
/// <param name="entity">The entity stopping flipping.</param>
[Serializable, NetSerializable]
public sealed class StopFlipEvent(NetEntity entity) : EntityEventArgs
{
    public NetEntity Entity = entity;
}
