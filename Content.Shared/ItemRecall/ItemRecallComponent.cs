using Robust.Shared.GameStates;

namespace Content.Shared.ItemRecall;

/// <summary>
/// Component for the ItemRecall action.
/// Used for marking a held item and recalling it back into your hand with second action use.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedItemRecallSystem))]
public sealed partial class ItemRecallComponent : Component
{
    /// <summary>
    /// The name the action should have while an entity is marked.
    /// </summary>
    [DataField]
    public LocId? WhileMarkedName = "item-recall-marked-name";

    /// <summary>
    /// The description the action should have while an entity is marked.
    /// </summary>
    [DataField]
    public LocId? WhileMarkedDescription = "item-recall-marked-description";

    /// <summary>
    /// The name the action starts with.
    /// This shouldn't be set in yaml.
    /// </summary>
    [DataField]
    public string? InitialName;

    /// <summary>
    /// The description the action starts with.
    /// This shouldn't be set in yaml.
    /// </summary>
    [DataField]
    public string? InitialDescription;

    /// <summary>
    /// The entity currently marked to be recalled by this action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? MarkedEntity;
}
