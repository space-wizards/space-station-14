using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedGunSystem))]
public sealed partial class SolutionAmmoProviderComponent : Component
{
    /// <summary>
    /// The solution where reagents are extracted from for the projectile.
    /// </summary>
    [DataField("solutionId", required: true), AutoNetworkedField]
    public string SolutionId = default!;

    /// <summary>
    /// How much reagent it costs to fire once.
    /// </summary>
    [DataField("fireCost"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float FireCost = 5;

    /// <summary>
    /// The amount of shots currently available.
    /// used for network predictions.
    /// </summary>
    [DataField("shots"), ViewVariables, AutoNetworkedField]
    public int Shots;

    /// <summary>
    /// The max amount of shots the gun can fire.
    /// used for network prediction
    /// </summary>
    [DataField("maxShots"), ViewVariables, AutoNetworkedField]
    public int MaxShots;

    /// <summary>
    /// The prototype that's fired by the gun.
    /// </summary>
    [DataField("proto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string Prototype = default!;
}
