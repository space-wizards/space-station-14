namespace Content.Server.Changeling;

public partial class ChangelingComponent : Component
{
    /// <summary>
    /// The maximum number of transformations that can be stored from sting extracts.
    /// Absorbing ignores this as it uses its own list.
    /// </summary>
    [DataField("maxExtractedTransformations"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxExtractedTransformations = 7;

    /// <summary>
    /// The transformations obtained by using extraction sting on people.
    /// Will cycle out oldest one if at MaxStingTransformations.
    /// You cannot sting fellow changelings, and they get a popup saying you tried.
    /// </summary>
    public LinkedList<Transformation> ExtractedTransformations = new();

    /// <summary>
    /// The identities obtained by absorbing people.
    /// There is no limit to this, as opposed to stinging.
    /// Absorbing people also grants memories such as uplink codes, objectives.
    /// This will also let you reset your abilities.
    /// Absorbing another changeling grants you all their abilities and evolution points.
    /// <summary>
    public List<Transformation> AbsorbedTransformations = new();

    /// <summary>
    /// Get the total number of absorbed transformations from extraction stinging and absorbing.
    /// </summary>
    [ViewVariables]
    public int TotalTransformations => ExtractedTransformations.Count + AbsorbedTransformations.Count;
}

/// <summary>
/// A transformation that can be used with the transform action.
/// Gained by stinging (limited) or absorbing (unlimited) something with AbsorbableComponent.
/// </summary>
public struct Transformation
{
    /// <summary>
    /// Name to set your player to when transforming.
    /// </summary>
    public string Name;

    /// <summary>
    /// Fingerprints to use when transforming.
    /// </summary>
    public string Fingerprint;

    /// <summary>
    /// DNA sequence to use when transforming.
    /// </summary>
    public string Dna;

    // TODO: physical appearance
}
