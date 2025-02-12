using Robust.Shared.GameStates;

namespace Content.Shared.ContainerHeld;

[RegisterComponent, NetworkedComponent]
public sealed partial class ContainerHeldComponent: Component
{
    /// <summary>
    ///     The amount of weight needed to be in the container
    ///     in order for it to toggle it's appearance
    ///     to ToggleVisuals.Toggled = true, and
    ///     SetHeldPrefix() to "full" instead of "empty".
    /// </summary>
    [DataField]
    public int Threshold { get; private set; } = 1;
}
