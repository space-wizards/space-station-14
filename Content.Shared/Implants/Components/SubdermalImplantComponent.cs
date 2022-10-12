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

    //TODO: Add logic for passive actions (IE tracking implant)

    //TODO: Add logic for reactive actions (IE macro bomb on death)
}
