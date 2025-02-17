using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Species.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ColdBloodedComponent : Component
{
    /// <summary>
    /// Maximum temperature that will be force entity to sleep. Than less temperature, than more chance to fall sleep.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SleepTemperature = 280f;

    [DataField, AutoNetworkedField]
    public float TemperatureCooficient = 0.5f;

    /// <summary>
    /// Minimum duration of the sleep.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinDuration = 1f;

    /// <summary>
    /// Maximum of the sleep in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxDuration = 5f;

    [DataField]
    public ProtoId<AlertPrototype> Alert = "ColdBlooded";
}
