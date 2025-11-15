using Robust.Shared.GameStates;

namespace Content.Shared.Instruments;

/// <summary>
/// Allows an instrument to swap between multiple sound programs (e.g. piano → bass).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SwappableInstrumentComponent : Component
{
    /// <summary>
    /// All available instrument programs this entity can cycle through.
    /// </summary>
    [AutoNetworkedField]
    public List<string> AvailablePrograms = new();

    /// <summary>
    /// The index of the currently selected program in <see cref="AvailablePrograms"/>.
    /// </summary>
    [AutoNetworkedField]
    public int CurrentIndex = 0;
}
