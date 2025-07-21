using Content.Shared.Audio;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Fluids;

/// <summary>
/// For entities that can clean up puddles
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AbsorbentComponent : Component
{
    public Dictionary<Color, float> Progress = new();

    /// <summary>
    /// Name for solution container, that should be used for absorbed solution storage and as source of absorber solution.
    /// Default is 'absorbed'.
    /// </summary>
    [DataField]
    public string SolutionName = "absorbed";

    /// <summary>
    /// How much solution we can transfer in one interaction.
    /// </summary>
    [DataField]
    public FixedPoint2 PickupAmount = FixedPoint2.New(100);

    [DataField]
    public SoundSpecifier PickupSound = new SoundPathSpecifier("/Audio/Effects/Fluids/watersplash.ogg")
    {
        Params = AudioParams.Default.WithVariation(SharedContentAudioSystem.DefaultVariation),
    };

    [DataField] public SoundSpecifier TransferSound =
        new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg")
        {
            Params = AudioParams.Default.WithVariation(SharedContentAudioSystem.DefaultVariation).WithVolume(-3f),
        };

    public static readonly SoundSpecifier DefaultTransferSound =
        new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg")
        {
            Params = AudioParams.Default.WithVariation(SharedContentAudioSystem.DefaultVariation).WithVolume(-3f),
        };

    /// <summary>
    /// Marker that absorbent component owner should try to use 'absorber solution' to replace solution to be absorbed.
    /// Target solution will be simply consumed into container if set to false.
    /// </summary>
    [DataField]
    public bool UseAbsorberSolution = true;
}
