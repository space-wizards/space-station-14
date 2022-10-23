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
    [Access(friends:typeof(SingularitySystem), Other=AccessPermissions.Read, Self=AccessPermissions.Read)]
    public float Energy = 180f;

    /// <summary>
    /// The rate at which this singularity loses energy over time.
    /// </summary>
    [DataField("energyLoss")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float EnergyDrain;

    /// <summary>
    /// The amount of time between energy level updates.
    /// </summary>
    [DataField("updatePeriod")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float UpdatePeriod = 1.0f;

    /// <summary>
    /// The amount of time that has elapsed since the last energy level update.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float _timeSinceLastUpdate = 0f;

#region Audio
    /// <summary>
    /// The sound that this singularity produces by existing.
    /// </summary>
    [DataField("ambiantSound")]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? AmbiantSound = new SoundPathSpecifier(
        "/Audio/Effects/singularity_form.ogg",
        AudioParams.Default.WithVolume(5).WithLoop(true).WithMaxDistance(20f)
    );

    /// <summary>
    /// The audio stream that plays the sound specified by <see cref="AmbiantSound"> on loop.
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
#endregion Audio

#region Serialization
    public override ComponentState GetComponentState()
    {
        return new SingularityComponentState(Level);
    }
#endregion Serialization

#region VV
    [ViewVariables(VVAccess.ReadWrite)]
    public float VVEnergy
    {
        get => Energy;
        set { IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SingularitySystem>().SetEnergy(this, value); }
    }
#endregion VV
}
