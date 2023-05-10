using Robust.Shared.GameStates;

namespace Content.Server.Changeling;

/// <summary>
/// Adds sting ability to the user.
/// Stinging can store up to 7 disguises.
/// </summary>
[RegisterComponent]
[Access(typeof(ChangelingSystem))]
public sealed class ChangelingComponent : Component
{
    /// <summary>
    /// ID of the currently active sting.
    /// Buy a sting ability and use its action to select it.
    /// </summary>
    public string ActiveSting = "ExtractionSting";

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
    [ViewVariables]
    public LinkedList<Transformation> ExtractedTransformations = new();

    /// <summary>
    /// The identities obtained by absorbing people.
    /// There is no limit to this, as opposed to stinging.
    /// Absorbing people also grants memories such as uplink codes, objectives.
    /// This will also let you reset your abilities.
    /// Absorbing another changeling grants you all their abilities and evolution points.
    /// <summary>
    [ViewVariables]
    public List<Transformation> AbsorbedTransformations = new();

    /// <summary>
    /// Get the total number of absorbed transformations from extraction stinging and absorbing.
    /// </summary>
    [ViewVariables]
    public int TotalTransformations => ExtractedTransformations.Count + AbsorbedTransformations.Count;

    /// <summary>
    /// Lets you refund all abilities for their evolution points.
    /// Evolution points are a unique currency stored on the player's StoreComponent.
    /// Can only reset after absorbing someone, does not stack with multiple absorbings.
    /// </summary>
    [DataField("canResetAbilities"), ViewVariables(VVAccess.ReadWrite)]
    public bool CanResetAbilities;

    /// <summary>
    /// Number of chemicals the changeling has, for some stings and abilities.
    /// Passively regenerates at a rate modified by certain abilities.
    /// </summary>
    [DataField("chemicals"), ViewVariables(VVAccess.ReadWrite)]
    public int Chemicals = 75;

    /// <summary>
    /// Maximum number of chemicals you can regenerate up to.
    /// Absorbing a changeling ignores this limit.
    /// </summary>
    [DataField("maxChemicals"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxChemicals = 75;

    /// <summary>
    /// Seconds it takes to regenerate a chemical.
    /// </summary>
    [DataField("chemicalRegenTime"), ViewVariables(VVAccess.ReadWrite)]
    public float ChemicalRegenTime = 1.0f;
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
}
