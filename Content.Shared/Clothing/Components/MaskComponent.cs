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
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsToggled;

    /// <summary>
    /// Equipped prefix to use after the mask was pulled down.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string EquippedPrefix = "up";

    /// <summary>
    /// When <see langword="false"/>, the mask will not be toggleable.
    /// </summary>
    [DataField("enabled"), AutoNetworkedField]
    public bool IsToggleable = true;

    /// <summary>
    /// When <see langword="true"/> will disable <see cref="IsToggleable"/> when folded
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DisableOnFolded;
}
