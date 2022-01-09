using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
///     When activated artifact will spawn an entity from prototype.
///     It could be an angry mob or some random item.
/// </summary>
[RegisterComponent]
public class SpawnArtifactComponent : Component
{
    public override string Name => "SpawnArtifact";

    [DataField("random")]
    public bool RandomPrototype = true;

    [DataField("possiblePrototypes")]
    public string[] PossiblePrototypes = {};

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("prototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? Prototype;

    [DataField("range")]
    public float Range = 0.5f;

    [DataField("maxSpawns")]
    public int MaxSpawns = 20;

    public int SpawnsCount = 0;
}
