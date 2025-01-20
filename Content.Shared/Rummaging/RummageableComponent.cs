using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Rummaging;

/// <summary>
/// This is used for entities that can be
/// rummaged through to get loot.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RummagingSystem))]
[AutoGenerateComponentState]
public sealed partial class RummageableComponent : Component
{
    /// <summary>
    /// Whether or not this entity has been rummaged through already.
    /// </summary>
    [DataField("looted"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool Looted;

    /// <summary>
    /// Whether or not this entity can be rummaged through multiple times.
    /// </summary>
    [DataField("relootable"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool Relootable = false;

    [DataField("relootableCooldown"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan RelootableCooldown = TimeSpan.FromSeconds(60);
    public TimeSpan NextRelootable;

    /// <summary>
    /// A weighted random entity prototype containing the different loot that rummaging can provide.
    /// Overrides the same setting on RummagingComponent.
    /// </summary>
    [DataField("rummageLoot", customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomEntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public string? RummageLoot = null;

    /// <summary>
    /// How long it takes to rummage through a rummageable container.
    /// </summary>
    [DataField("rummageDuration"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float RummageDuration = 3f;

    /// <summary>
    /// Sound played on rummage completion.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound = new SoundCollectionSpecifier("storageRustle");
}
