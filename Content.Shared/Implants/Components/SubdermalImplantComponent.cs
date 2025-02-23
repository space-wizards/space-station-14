using Content.Shared.Actions;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants.Components;

/// <summary>
/// Subdermal implants get stored in a container on an entity and grant the entity special actions
/// The actions can be activated via an action, a passive ability (ie tracking), or a reactive ability (ie on death) or some sort of combination
/// They're added and removed with implanters
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SubdermalImplantComponent : Component
{
    /// <summary>
    /// Used where you want the implant to grant the owner an instant action.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("implantAction")]
    public EntProtoId? ImplantAction;

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    /// <summary>
    /// The entity this implant is inside
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? ImplantedEntity;

    /// <summary>
    /// Should this implant be removeable?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("permanent"), AutoNetworkedField]
    public bool Permanent = false;

    /// <summary>
    /// Target whitelist for this implant specifically.
    /// Only checked if the implanter allows implanting on the target to begin with.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Target blacklist for this implant specifically.
    /// Only checked if the implanter allows implanting on the target to begin with.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
    
    /// <summary>
    /// If set, this ProtoId is used when attempting to draw the implant instead.
    /// Useful if the implant is a child to another implant and you don't want to differentiate between them when drawing.
    /// </summary>
    [DataField]
    public EntProtoId? DrawableProtoIdOverride;
}

/// <summary>
/// Used for opening the storage implant via action.
/// </summary>
public sealed partial class OpenStorageImplantEvent : InstantActionEvent
{

}

public sealed partial class UseFreedomImplantEvent : InstantActionEvent
{

}

/// <summary>
/// Used for triggering trigger events on the implant via action
/// </summary>
public sealed partial class ActivateImplantEvent : InstantActionEvent
{

}

/// <summary>
/// Used for opening the uplink implant via action.
/// </summary>
public sealed partial class OpenUplinkImplantEvent : InstantActionEvent
{

}

public sealed partial class UseScramImplantEvent : InstantActionEvent
{

}

public sealed partial class UseDnaScramblerImplantEvent : InstantActionEvent
{

}
