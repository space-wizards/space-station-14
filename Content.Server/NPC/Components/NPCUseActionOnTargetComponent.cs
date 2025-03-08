using Content.Server.NPC.Systems;
using Content.Shared.Actions;
using Content.Shared.NPC;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Components;

/// <summary>
/// This is used for an NPC that constantly tries to use an action on a given target.
/// </summary>
[RegisterComponent, Access(typeof(NPCUseActionOnTargetSystem))]
public sealed partial class NPCUseActionOnTargetComponent : Component
{
    [DataField("actions")]
    public List<NPCActionsData> Actions = new();
}

/// <summary>
/// Contains the actions an NPC is allowed to use, and what they can use them on
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class NPCActionsData
{
    [DataField(required: true)]
    public EntProtoId ActionId;
    /// <summary>
    /// HTN blackboard key for the target entity
    /// </summary>
    [DataField] public string TargetKey = "Target";
    /// <summary>
    /// A place to store the EntityUID of our action (might not be necessary make sure to check stupid)
    /// </summary>
    [DataField] public EntityUid? ActionEnt;
    /// <summary>
    /// If true, will not give the entity a new action but will instead try to find a matching action the entity can use. If false, the entity will get a new usable action.
    /// Currently doesn't have logic behind it :^)
    /// </summary>
    [DataField] public bool Reference;
}
