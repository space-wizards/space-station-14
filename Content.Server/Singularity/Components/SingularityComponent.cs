using Content.Shared.Singularity.Components;
using Content.Server.Singularity.EntitySystems;
using Robust.Shared.Audio;

namespace Content.Server.Singularity.Components;

/// <summary>
/// The server-side version of <see cref="SharedSingularityComponent">.
/// Primarily managed by <see cref="SingularitySystem">.
/// </summary>
[RegisterComponent]
[ComponentReference(typeof(SharedSingularityComponent))]
public sealed class SingularityComponent : SharedSingularityComponent
{
    /// <summary>
    /// The amount of energy this singularity contains.
    /// If you want to set this go through <see cref="SingularitySystem.SetEnergy"/>
    /// </summary>
    [DataField("energy")]
    [Access(friends:typeof(SingularitySystem))]
    public float Energy = 180f;

    /// <summary>
    /// The rate at which this singularity loses energy over time.
    /// </summary>
    [DataField("energyLoss")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float EnergyDrain;

    #region Audio

    /// <summary>
    /// The sound that this singularity produces by existing.
    /// </summary>
    [DataField("ambientSound")]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? AmbientSound = new SoundPathSpecifier(
        "/Audio/Effects/singularity_form.ogg",
        AudioParams.Default.WithVolume(5).WithLoop(true).WithMaxDistance(20f)
    );

    /// <summary>
    /// The audio stream that plays the sound specified by <see cref="AmbientSound"> on loop.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public IPlayingAudioStream? AmbientSoundStream = null;

    /// <summary>
    ///     The sound that the singularity produces when it forms.
    /// </summary>
    [DataField("formationSound")]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? FormationSound = null;

    /// <summary>
    ///     The sound that the singularity produces when it dissipates.
    /// </summary>
    [DataField("dissipationSound")]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? DissipationSound = new SoundPathSpecifier(
        "/Audio/Effects/singularity_collapse.ogg",
        AudioParams.Default
    );

    #endregion Audio

    #region Update Timing

    /// <summary>
    /// The amount of time that should elapse between automated updates to this singularity.
    /// </summary>
    [DataField("updatePeriod")]
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(SingularitySystem))]
    public TimeSpan TargetUpdatePeriod { get; internal set; } = TimeSpan.FromSeconds(1.0);

    /// <summary>
    /// The next time this singularity should be updated by <see cref="SingularitySystem"/>
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(SingularitySystem))]
    public TimeSpan NextUpdateTime { get; internal set; } = default!;

    /// <summary>
    /// The last time this singularity was be updated by <see cref="SingularitySystem"/>
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(SingularitySystem))]
    public TimeSpan LastUpdateTime { get; internal set; } = default!;

    #endregion Update Timing
}
