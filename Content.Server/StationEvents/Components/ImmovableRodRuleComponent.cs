using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(ImmovableRodRule))]
public sealed partial class ImmovableRodRuleComponent : Component
{
    [DataField("rodPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string RodPrototype = "ImmovableRodKeepTilesStill";

    /// <summary>
    ///     List of rod variants.
    /// </summary>
    [DataField]
    public List<EntProtoId> RodRandomPrototypes = new();

    /// <summary>
    ///     Probability for rod to be a variant.
    /// </summary>
    [DataField]
    public float RodRandomProbability = 0.05f;
}
