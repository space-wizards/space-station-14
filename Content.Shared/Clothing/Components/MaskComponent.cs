using Content.Shared.Clothing.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(MaskSystem))]
public sealed partial class MaskComponent : Component
{
    /// <summary>
    /// Action for toggling a mask (e.g., pulling the mask down or putting it back up)
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId ToggleAction = "ActionToggleMask";

    /// <summary>
    /// Action for toggling a mask (e.g., pulling the mask down or putting it back up)
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;

    /// <summary>
    /// Whether the mask is currently toggled (e.g., pulled down).
    /// This generally disables some of the mask's functionality.
    /// This is different from <see cref="IsEnabled"/>. As to what the difference is, I have no idea what
    /// <see cref="IsEnabled"/> is for so you're on your own.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsToggled;

    [DataField, AutoNetworkedField]
    public string EquippedPrefix = "toggled";

    /// <summary>
    /// When <see langword="true"/> will function normally, otherwise will not react to events
    /// </summary>
    [DataField("enabled"), AutoNetworkedField]
    public bool IsEnabled = true;

    /// <summary>
    /// When <see langword="true"/> will disable <see cref="IsEnabled"/> when folded
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DisableOnFolded;
}
