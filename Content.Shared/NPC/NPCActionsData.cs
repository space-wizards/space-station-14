using Robust.Shared.Prototypes;
using Content.Shared.Actions;

namespace Content.Shared.NPC;

/// <summary>
/// Contains the actions an NPC is allowed to use, and what they can use them on
/// </summary>
[Serializable]
[DataDefinition]
public partial struct NPCActionsData
{
    [DataField(required: true)]
    public EntProtoId<EntityWorldTargetActionComponent> ActionId;
    /// <summary>
    /// HTN blackboard key for the target entity
    /// </summary>
    [DataField] public string TargetKey = "Target";
    /// <summary>
    /// A place to store the EntityUID of our action (might not be necessary make sure to check stupid)
    /// </summary>
    [DataField] public EntityUid? ActionEnt;
}
