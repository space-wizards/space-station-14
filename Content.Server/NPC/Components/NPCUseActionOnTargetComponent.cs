using Content.Server.NPC.Systems;
using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Components;

/// <summary>
/// This is used for an NPC that constantly tries to use an action on a given target.
/// </summary>
[RegisterComponent, Access(typeof(NPCUseActionOnTargetSystem))]
public sealed partial class NPCUseActionOnTargetComponent : Component
{
    /// <summary>
    /// HTN blackboard key for the target entity
    /// </summary>
    [DataField]
    public string TargetKey = "Target";

    /// <summary>
    /// Action that's going to attempt to be used.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId<EntityWorldTargetActionComponent>, float> Actions = new();

    [DataField]
    public Dictionary<EntityUid, float> ActionsEntities = new();

    [DataField]
    public (TimeSpan Start, TimeSpan End)? Cooldown;

    [DataField]
    public TimeSpan UseDelay = TimeSpan.FromSeconds(8);

}
