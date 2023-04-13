using Robust.Shared.GameStates;

namespace Content.Shared.UserInterface;

/// <summary>
/// Indicates that the entity's ActivatableUI requires power or else it closes.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class ActivatableUIBatteryComponent : Component
{
    /// <summary>
    /// The key for the UI.
    /// </summary>
    [DataField("key", required: true)]
    public Enum UiKey = default!;

    /// <summary>
    /// How much the entity draws while the UI is open.
    /// Set to 0 if you just wish to check for power upon opening the UI.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("drawRate")]
    public float DrawRate = 1f;

    /// <summary>
    /// How much power is used whenever the entity is "used".
    /// This is used to ensure the UI won't open again without a minimum use power.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("useRate")]
    public float UseRate = 0f;
}
