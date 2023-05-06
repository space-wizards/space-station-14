using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SolutionAmmoProviderComponent : Component
{
    [DataField("solutionId", required: true), AutoNetworkedField]
    public string SolutionId = default!;

    /// <summary>
    /// How much reagent it costs to fire once.
    /// </summary>
    [DataField("fireCost"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float FireCost = 10;

    [ViewVariables, AutoNetworkedField]
    public int Shots;

    [ViewVariables, AutoNetworkedField]
    public int MaxShots;

    [ViewVariables(VVAccess.ReadWrite), DataField("proto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;
}
