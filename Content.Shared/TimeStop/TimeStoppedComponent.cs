using Robust.Shared.GameStates;

namespace Content.Shared.TimeStop;

/// <summary>
/// Marks that an entity has already been frozen
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TimeStoppedComponent : Component
{

    /// <summary>
    /// How many stops are being applied at once.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int StopCount;
}
