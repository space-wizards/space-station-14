using Content.Shared.Disease;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
/// <summary>
///     Spawn a random disease at regular intervals when artifact activated.
/// </summary>
[RegisterComponent]
public sealed class DiseaseArtifactComponent : Component
{
    /// <summary>
    /// The diseases that the artifact can use.
    /// </summary>
    [DataField("diseasePrototype", customTypeSerializer: typeof(PrototypeIdListSerializer<DiseasePrototype>))]
    public List<string> DiseasePrototypes = new();

    /// <summary>
    /// Disease the artifact will spawn
    /// Picks a random one from its list
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public DiseasePrototype? SpawnDisease;

    /// <summary>
    /// How far away it will check for people
    /// If empty, picks a random one from its list
    /// </summary>
    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    public float Range = 5f;
}
