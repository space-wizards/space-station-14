using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;

namespace Content.Shared.Implants.Components;

[RegisterComponent]
public sealed class SubdermalImplantComponent : Component
{
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

    /// <summary>
    /// Should this implant be removeable?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("permanent")]
    public bool Permanent = false;
}

public sealed class OpenStorageImplantEvent : InstantActionEvent
{

}
