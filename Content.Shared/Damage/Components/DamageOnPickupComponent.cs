using Content.Shared.Damage.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent]

/// <summary>
/// Entities with this component will trigger checks when this entity is placed into a container.
/// </summary>
public sealed partial class DamageOnPickupComponent : Component
{
    /// <summary>
    /// Sound to play when this item was failed to be inserted
    /// </summary>
    [DataField("failsound")]
    public SoundSpecifier FailSound = default!;

    /// <summary>
    /// Speed at which to eject this item away from an entity
    /// </summary>
    [DataField("throwspeed")]
    public int ThrowSpeed = 10;

    /// <summary>
    /// Boolean to check if the container should receive damage when this item was failed to be inserted
    /// </summary>
    [DataField("takedamage")]
    public bool TakeDamage = true;

    /// <summary>
    /// Damage applied to the container's owner when this item was failed to be inserted
    /// </summary>
    [DataField("damage")]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Boolean to eject the item from the entity if this item was failed to be inserted
    /// </summary>
    [DataField("throw")]
    public bool Throw = true;
}
