using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.RatKing;

/// <summary>
/// This is used for entities that can be
/// rummaged through by the rat king to get loot.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRatKingSystem))]
[AutoGenerateComponentState]
public sealed partial class RatKingRummageableComponent : Component
{
    /// <summary>
    /// Whether or not this entity has been rummaged through already.
    /// </summary>
    [DataField("looted"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool Looted;

    /// <summary>
    /// How long it takes to rummage through a rummageable container.
    /// </summary>
    [DataField("rummageDuration"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float RummageDuration = 3f;

    /// <summary>
    /// A weighted random entity prototype containing the different loot that rummaging can provide.
    /// </summary>
    [DataField("rummageLoot", customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomEntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public string RummageLoot = "RatKingLoot";

    /// <summary>
    /// Sound played on rummage completion.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound = new SoundCollectionSpecifier("storageRustle");
}
