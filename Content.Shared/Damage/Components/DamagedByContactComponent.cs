using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Damage.Components;

/// <summary>
/// MarkerComponent that indicates that this entity is currently colliding with and taking damage from
/// and entity with <see cref="DamageContactsComponent"/>.
/// </summary>
[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class DamagedByContactComponent : Component
{
    /// <summary>
    /// Time at which the entity will next take damage.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextSecond = TimeSpan.Zero;

    /// <summary>
    /// The amount of damage taken each second.
    /// This is automatically copied over from <see cref="DamageContactsComponent"/> on collision.
    /// </summary>
    /// <remarks>
    /// TODO: This won't work if you are colliding with multiple damage sources at the same time.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public DamageSpecifier? Damage;
}
