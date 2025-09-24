using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Disease;

/// <summary>
/// Represents a collected disease sample, storing disease prototype IDs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DiseaseSampleComponent : Component
{
    /// <summary>
    /// Determines whether there is a sample on the swab.
    /// </summary>
    [DataField]
    public bool HasSample;

    /// <summary>
    /// Display name of the sampled subject at the time of sampling.
    /// </summary>
    [DataField]
    public string? SubjectName;

    /// <summary>
    /// DNA string of the sampled subject at the time of sampling.
    /// </summary>
    [DataField]
    public string? SubjectDNA;

    /// <summary>
    /// Disease prototype IDs captured by this sample (from a swab etc.).
    /// </summary>
    [DataField]
    public List<string> Diseases = [];

    /// <summary>
    /// Optional per-disease stage captured at the moment of sampling.
    /// </summary>
    [DataField]
    public Dictionary<string, int> Stages = [];
}
