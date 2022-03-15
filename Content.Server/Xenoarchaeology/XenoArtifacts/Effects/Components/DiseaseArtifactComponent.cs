using Content.Shared.Disease;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
/// <summary>
///     Spawn a random disease at regular intervals when artifact activated.
/// </summary>
[RegisterComponent]
public sealed class DiseaseArtifactComponent : Component
{
    public override string Name => "DiseaseArtifact";
    /// <summary>
    /// Disease the artifact will spawn
    /// If empty, picks a random one from its list
    /// </summary>
    [DataField("disease", customTypeSerializer: typeof(PrototypeIdSerializer<DiseasePrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public string SpawnDisease = string.Empty;
    /// <summary>
    /// How far away it will check for people
    /// If empty, picks a random one from its list
    /// </summary>
    [DataField("range")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Range = 5f;
    [ViewVariables(VVAccess.ReadWrite)]
    public DiseasePrototype ResolveDisease = default!;
    [ViewVariables(VVAccess.ReadWrite)]
    public readonly IReadOnlyList<string> ArtifactDiseases = new[]
    {
        "VanAusdallsRobovirus",
        "OwOnavirus",
        "BleedersBite",
        "Ultragigacancer",
        "AMIV"
    };
}
