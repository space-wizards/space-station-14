using Robust.Shared.GameStates;

namespace Content.Shared.Species.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ColdBloodedComponent : Component
{
    /// <summary>
    /// Maximum temperature that will be force entity to sleep. Than less temperature, than more chance to fall sleep.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SleepTemperature = 280f;

    /// <summary>
    /// Duration of the sleep.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Duration = 0.1f;
}
