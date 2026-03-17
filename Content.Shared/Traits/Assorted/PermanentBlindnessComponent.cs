using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This is used for making something blind forever.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PermanentBlindnessComponent : Component
{
    /// <summary>
    /// How damaged should their eyes be? Set 0 for maximum damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Blindness = 0;
}

