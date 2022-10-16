using Content.Shared.Singularity;
using Content.Shared.Singularity.Components;
using Content.Server.Singularity.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Singularity.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedSingularityComponent))]
public sealed class SingularityComponent : SharedSingularityComponent
{
    /// <summary>
    ///
    /// </summary>
    [Access(friends:typeof(SingularitySystem))]
    public float _energy = 180;

    /// <summary>
    ///
    /// </summary>
    [DataField("energy")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Energy
    {
        get => _level;
        set { IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SingularitySystem>().SetSingularityEnergy(this, value); }
    }

    /// <summary>
    ///     The rate at which this singularity loses energy over time.
    /// </summary>
    [DataField("energyLoss")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float EnergyDrain;

    /// <summary>
    ///     The number of seconds between energy level updates.
    /// </summary>
    [DataField("updatePeriod")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float UpdatePeriod = 1.0f;

    /// <summary>
    ///     The amount of time that has passed since the last energy level update.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float _timeSinceLastUpdate = float.PositiveInfinity;

    /// <summary>
    ///     The sound that this singularity produces by existing.
    /// </summary>
    [DataField("ambiantSound")]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? AmbiantSound = new SoundPathSpecifier(
        "/Audio/Effects/singularity_form.ogg",
        AudioParams.Default.WithVolume(5).WithLoop(true).WithMaxDistance(20f)
    );

    /// <summary>
    ///     The audio stream that plays the above sound on loop.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public IPlayingAudioStream? AmbiantSoundStream = null;

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


    public override ComponentState GetComponentState()
    {
        return new SingularityComponentState(Level);
    }
}
