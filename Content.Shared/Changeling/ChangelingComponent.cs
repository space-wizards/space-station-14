using Robust.Shared.Audio;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Content.Shared.Changeling;
using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ChangelingComponent : Component
{
    /// <summary>
    /// Number of chemicals the changeling has, for some stings and abilities.
    /// Passively regenerates at a rate modified by certain abilities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Chemicals = 0;

    /// <summary>
    /// Maximum number of chemicals you can regenerate up to.
    /// Absorbing people can increase this limit.
    /// </summary>
    [DataField]
    public int MaxChemicals = 75;

    /// <summary>
    /// Seconds it takes to regenerate a chemical.
    /// </summary>
    [DataField]
    public float ChemicalRegenTime = 1.0f;

    /// <summary>
    /// The maximum amount of DNA strands a ling can have at one time
    /// </summary>
    [DataField]
    public int DNAStrandCap = 10;

    /// <summary>
    /// List of stolen DNA
    /// </summary>
    [DataField]
    public List<TransformData> StoredDNA = [];

    /// <summary>
    /// The DNA index that the changeling currently has selected
    /// </summary>
    [DataField]
    public int SelectedDNA = 0;

    /// <summary>
    /// The stage of absorbing that the changeling is on. Maximum of 2 stages.
    /// </summary>
    [DataField]
    public int AbsorbStage = 0;

    /// <summary>
    /// The amount of evolution points the changeling gains when they absorb another changeling.
    /// </summary>
    [DataField]
    public float AbsorbedChangelingPointsAmount = 5.0f;

    /// <summary>
    /// Sound that plays when the ling uses the regenerate ability.
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundRegenerate = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    [DataField]
    public SoundSpecifier? SoundFlesh = new SoundPathSpecifier("/Audio/Effects/blobattack.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    [DataField]
    public float Accumulator = 0f;
}

public struct TransformData
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

    /// <summary>
    /// Humanoid appearance to use when transforming.
    /// </summary>
    public HumanoidAppearanceComponent HumanoidAppearanceComp;
}