using Content.Shared.Actions;

namespace Content.Shared.Implants.Components;

/// <summary>
/// Subdermal implants get stored in a container on an entity and grant the entity special actions
/// The actions can be activated via an action, a passive ability (ie tracking), or a reactive ability (ie on death) or some sort of combination
/// They're added and removed with implanters
/// </summary>
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

/// <summary>
/// Used for opening the storage implant via action.
/// </summary>
public sealed class OpenStorageImplantEvent : InstantActionEvent
{

}

/// <summary>
/// Used for triggering micro bombs via action
/// </summary>
public sealed class ActivateMicroBombImplantEvent : InstantActionEvent
{

}
