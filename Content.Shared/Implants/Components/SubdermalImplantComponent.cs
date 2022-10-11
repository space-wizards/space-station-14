using Content.Shared.Actions.ActionTypes;

namespace Content.Shared.Implants.Components;

[RegisterComponent]
public sealed class SubdermalImplantComponent : Component
{
    //TODO: Add logic for granting instant actions to the owner of the subdermal implant

    /// <summary>
    /// Used where you want the implant to grant the owner an instant action.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("implantAction")]
    public string? ImplantAction;

    public EntityUid? EntityUid;

    //TODO: Add logic for passive actions (IE tracking implant)

    //TODO: Add logic for reactive actions (IE macro bomb on death)
}
