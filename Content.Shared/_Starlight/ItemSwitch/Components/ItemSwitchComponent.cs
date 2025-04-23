using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Item.ItemToggle.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ItemSwitchComponent : Component
{
    /// <summary>
    ///     The item's toggle state.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string State;

    [DataField(readOnly: true)]
    public Dictionary<string, ItemSwitchState> States = [];

    /// <summary>
    /// Can the entity be activated in the world.
    /// </summary>
    [DataField]
    public bool OnActivate = true;

    /// <summary>
    /// If this is set to false then the item can't be toggled by pressing Z.
    /// Use another system to do it then.
    /// </summary>
    [DataField]
    public bool OnUse = true;

    /// <summary>
    ///     Whether the item's toggle can be predicted by the client.
    /// </summary>
    /// /// <remarks>
    /// If server-side systems affect the item's toggle, like charge/fuel systems, then the item is not predictable.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public bool Predictable = true;
}
[DataDefinition]
public sealed partial class ItemSwitchState : BoundUserInterfaceMessage
{
    [DataField]
    public string Verb;

    [DataField]
    public SoundSpecifier? SoundStateActivate;

    [DataField]
    public SoundSpecifier? SoundFailToActivate;

    [DataField]
    public ComponentRegistry? Components;

    [DataField]
    public bool RemoveComponents = true;
    
    [DataField]
    public bool Hiden = false;

    [DataField]
    public SpriteSpecifier? Sprite;
}

/// <summary>
/// Raised directed on an entity when its ItemToggle is attempted to be activated.
/// </summary>
[ByRefEvent]
public record struct ItemSwitchAttemptEvent()
{
    public bool Cancelled = false;
    public required readonly EntityUid? User { get; init; }
    public required readonly string State { get; init; }
    /// <summary>
    /// Pop-up that gets shown to users explaining why the attempt was cancelled.
    /// </summary>
    public string? Popup { get; set; }
}

/// <summary>
/// Raised directed on an entity any sort of toggle is complete.
/// </summary>
[ByRefEvent]
public readonly record struct ItemSwitchedEvent()
{
    public required readonly bool Predicted { get; init; }
    public required readonly string State { get; init; }
    public required readonly EntityUid? User { get; init; }
}
