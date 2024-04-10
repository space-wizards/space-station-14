using Robust.Shared.Audio;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Content.Shared.Changeling;

namespace Content.Shared.Changeling.Components;

[RegisterComponent]
[AutoGenerateComponentState]
public abstract partial class ChangelingComponent : Component
{

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