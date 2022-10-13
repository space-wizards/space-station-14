using Content.Shared.Actions.ActionTypes;

namespace Content.Shared.Implants.Components;

[RegisterComponent]
public sealed class SubdermalImplantComponent : Component
{
    //TODO: Look into the implant action and see how to get it properly working.

    /// <summary>
    /// Used where you want the implant to grant the owner an instant action.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("implantAction")]
    public string? ImplantAction;

    /// <summary>
    /// The entity this implant is inside
    /// </summary>
    public EntityUid? EntityUid;

    //TODO: Add things like unremoveable implant checks
}
