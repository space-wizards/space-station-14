using Robust.Shared.GameStates;

namespace Content.Shared.Cabinet;

/// <summary>
/// Item cabinet that cannot be opened if it has an item inside.
/// The only way to open it after that is to emag it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SealingCabinetComponent : Component
{
    /// <summary>
    /// Popup shown when trying to open the cabinet once sealed.
    /// </summary>
    [DataField(required: true)]
    public LocId SealedPopup = string.Empty;

    /// <summary>
    /// Set to false to disable emag unsealing.
    /// </summary>
    [DataField]
    public bool Emaggable = true;
}
