using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Rummaging;

/// <summary>
/// This is used for entities that can rummage for loot.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RummagingSystem))]
[AutoGenerateComponentState]
public sealed partial class RummagingComponent : Component
{
    /// <summary>
    /// A weighted random entity prototype containing the different loot that rummaging can provide. 
    /// Defining this on the rummager so different things can get different stuff out of the same container type.
    /// Can be overridden on the entity with Rummageable. 
    /// </summary>
    [DataField("rummageLoot", customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomEntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public string RummageLoot = "RatKingLoot";

    [DataField("rummageVerb"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public LocId RummageVerb = "rat-king-rummage-text";

    /// <summary>
    /// Rummage speed multiplier.
    /// </summary>
    [DataField("rummageModifier"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float RummageModifier = 1f;
}
