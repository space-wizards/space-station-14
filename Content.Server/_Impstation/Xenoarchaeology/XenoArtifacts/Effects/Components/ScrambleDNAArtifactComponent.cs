namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// Scrambles DNA of some number of people within some range, possibly outside of their species
/// </summary>
[RegisterComponent]
public sealed partial class ScrambleDNAArtifactComponent : Component
{
    /// <summary>
    /// Distance from the artifact find people to scramble DNA
    /// </summary>
    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    public float Range = 6f;

    /// <summary>
    /// Number of people to DNA scramble
    /// </summary>
    [DataField("count"), ViewVariables(VVAccess.ReadWrite)]
    public int Count = 1;

    /// <summary>
    /// Force scrambling to respect initial species
    /// </summary>
    [DataField("withinSpecies"), ViewVariables(VVAccess.ReadWrite)]
    public bool WithinSpecies = true;

    /// <summary>
    /// if WithinSpecies is false, what species cannot be rolled
    /// </summary>
    [DataField("excludedSpecies"), ViewVariables(VVAccess.ReadWrite)]
    public HashSet<string>? ExcludedSpecies = null;


}
