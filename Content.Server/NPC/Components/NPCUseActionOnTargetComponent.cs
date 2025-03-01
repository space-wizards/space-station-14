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
    /// <summary>
    /// HTN blackboard key for the target entity
    /// </summary>
    [DataField]
    public string TargetKey = "Target";

    /// <summary>
    /// Action that's going to attempt to be used.
    /// </summary>
    [DataField]
    public EntProtoId<EntityWorldTargetActionComponent> ActionId;

    [DataField]
    public EntityUid? ActionEnt;
}
