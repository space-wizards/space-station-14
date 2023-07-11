using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Containers.OnCollide;

/// <summary>
/// When this component is added we remove everything from the container
/// when the entity collides (and the collidable whitelist passes if given)
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(RemoveFromContainerOnCollideSystem))]
public sealed class RemoveFromContainerOnCollideComponent : Component
{
    /// <summary>
    /// ID of the target container
    /// </summary>
    [DataField("container", required: true)]
    public string Container = default!;

    /// <summary>
    /// Entities we can collide with without removing from the container
    /// </summary>
    [DataField("collidableEntities")]
    public EntityWhitelist? CollidableEntities;

    /// <summary>
    /// Min velocity we need to remove everything in the container.
    /// Represented in meters/tiles per second
    /// </summary>
    [DataField("requiredVelocity")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float RequiredVelocity;

    /// <summary>
    /// Whether or not try to remove everything inside the container
    /// Only remove one thing if false
    /// </summary>
    [DataField("removeEverything")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool RemoveEverything = true;

    /// <summary>
    /// Whether or not we should remove/unbuckle strapped
    /// entities when we collide
    /// </summary>
    [DataField("removeStrapped")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool RemoveStrapped = true;

    /// <summary>
    /// Whether or not to throw the container contents around after colliding
    /// </summary>
    [DataField("ejectAfterRemove")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool EjectAfterRemove = true;

    /// <summary>
    /// Min and max angles to give to our random throws
    /// </summary>
    [DataField("ejectRange")]
    [ViewVariables(VVAccess.ReadWrite)]
    public (float Min, float Max) EjectRange = (1f, 2f);

    /// <summary>
    /// Force of our throws
    /// </summary>
    [DataField("ejectStrength")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float EjectStrength = 5f;

    /// <summary>
    /// Pushback we get when throwing
    /// </summary>
    [DataField("ejectPushbackRatio")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float EjectPushbackRatio;
}
