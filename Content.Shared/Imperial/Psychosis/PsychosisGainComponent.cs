using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This component is used for Psychosis, which causes auditory hallucinations.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedPsychosisGainSystem))]
public sealed partial class PsychosisGainComponent : Component
{
    /// <summary>
    /// The maximum time between incidents in seconds
    /// </summary>
    [DataField("resist"), ViewVariables(VVAccess.ReadWrite)]
    public float Resist = 1f;

    [DataField("status"), ViewVariables(VVAccess.ReadWrite)]
    public float Status = 0f;
    public TimeSpan Cooldown = TimeSpan.FromSeconds(1);
    public TimeSpan NextUpdate = TimeSpan.FromSeconds(0);
}
