using Content.Shared.Clothing.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(MaskSystem))]
public sealed partial class MaskComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId ToggleAction = "ActionToggleMask";

    /// <summary>
    /// This mask can be toggled (pulled up/down)
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;

    [DataField, AutoNetworkedField]
    public bool IsToggled;
}
