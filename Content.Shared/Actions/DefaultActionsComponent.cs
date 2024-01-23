using Robust.Shared.Prototypes;

namespace Content.Shared.Actions;

/// <summary>
/// Grants a user a list of actions on mapinit.
/// The action events must be handled by other systems.
/// </summary>
[RegisterComponent, Access(typeof(DefaultActionsSystem))]
public sealed partial class DefaultActionsComponent : Component
{
    /// <summary>
    /// Action id and entity to be added on mapinit.
    /// </summary>
    [DataField(required: true)]
    public List<ActionPair> Actions = new();
}

[DataDefinition]
public sealed partial class ActionPair
{
    /// <summary>
    /// Action entity prototype to be added.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Id = string.Empty;

    /// <summary>
    /// Action entity that has been added.
    /// </summary>
    [DataField]
    public EntityUid? Entity;
}
