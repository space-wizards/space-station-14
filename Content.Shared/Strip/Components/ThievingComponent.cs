using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Strip.Components;

/// <summary>
/// Give this to an entity when you want to decrease stripping times
/// </summary>
[RegisterComponent]
[Virtual]
public partial class ThievingComponent : Component
{
    /// <summary>
    /// How much the strip time should be shortened by
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float StripTimeReduction = 0.5f;

    /// <summary>
    /// Should it notify the user if they're stripping a pocket?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Stealthy;
}

[RegisterComponent]
public sealed partial class ToggleableThievingComponent : ThievingComponent
{

    [DataField]
    public EntProtoId ThievingToggleActionProto = "ActionToggleThieving";

    [DataField]
    public EntityUid? ThievingToggleAction;
}

/// <summary>
/// When an item with this component is used, grant the user the ToggleableThievingComponent
/// </summary>
[RegisterComponent]
public sealed partial class ThievingGranterComponent : ThievingComponent
{

}

public sealed partial class ToggleThievingActionEvent : InstantActionEvent
{

}
