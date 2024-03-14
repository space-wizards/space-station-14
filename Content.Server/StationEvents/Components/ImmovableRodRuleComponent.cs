using Content.Server.StationEvents.Events;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(ImmovableRodRule))]
public sealed partial class ImmovableRodRuleComponent : Component
{
    [DataField]
    public EntProtoId RodPrototype = "ImmovableRodKeepTilesStill";

    /// <summary>
    ///     List of rod variants.
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry> RodRandomPrototypes = new();

    /// <summary>
    ///     Probability for rod to be a variant.
    /// </summary>
    [DataField]
    public float RodRandomProbability = 0.05f;
}
