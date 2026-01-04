using Content.Server.NPC.Systems;
using Content.Shared.Actions.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Components;

/// <summary>
/// This is used for an NPC that constantly tries to use an action on a given target.
/// </summary>
[RegisterComponent, Access(typeof(NpcUseActionOnTargetSystem))]
public sealed partial class NPCUseActionOnTargetComponent : Component
{
    /// <summary>
    /// List of actions that the entity is allowed to use, based on prototype.
    /// </summary>
    [DataField]
    public List<NpcActionData> Actions = new();
}

/// <summary>
/// Contains the actions an NPC is allowed to use, and what they can use them on
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class NpcActionData
{
    /// <summary>
    /// Prototype our Action is built from
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<ActionComponent> ActionId;

    /// <summary>
    /// HTN blackboard key for the target entity
    /// </summary>
    [DataField] public string TargetKey = "Target";

    /// <summary>
    /// The entityUid of our action
    /// </summary>
    [DataField] public EntityUid? ActionEnt;

    /// <summary>
    /// If true, will not give the entity a new action but will instead try to find a matching action the entity can use. If false, the entity will get a new usable action.
    /// </summary>
    [DataField] public bool Ref;
}
