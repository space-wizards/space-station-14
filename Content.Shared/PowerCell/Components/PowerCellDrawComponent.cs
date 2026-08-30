using Robust.Shared.GameStates;

namespace Content.Shared.PowerCell.Components;

/// <summary>
/// Indicates that the entity's ActivatableUI requires power or else it closes.
/// </summary>
/// <remarks>
/// With ActivatableUI it will activate and deactivate when the ui is opened and closed, drawing power inbetween.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(PowerCellSystem))]
public sealed partial class PowerCellDrawComponent : Component
{
    /// <summary>
    /// Whether drawing is enabled.
    /// Having no cell will still disable it.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables]
    public bool Enabled = true;

    /// <summary>
    /// How much the entity draws while the UI is open (in Watts).
    /// Set to 0 if you just wish to check for power upon opening the UI.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables]
    public float DrawRate = 1f;

    /// <summary>
    /// How much power is used whenever the entity is "used" (in Joules).
    /// This is used to ensure the UI won't open again without a minimum use power.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float UseCharge;
}
