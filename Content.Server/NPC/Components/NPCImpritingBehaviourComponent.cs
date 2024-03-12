using Content.Server.NPC.Systems;

namespace Content.Server.NPC.Components;
/// <summary>
/// A component that makes the entity friendly to the nearest creature it sees on init.
/// </summary>
[RegisterComponent]
[Access(typeof(NPCImpritingBehaviourSystem))]
public sealed partial class NPCImpritingBehaviourComponent : Component
{
    [DataField]
    public HashSet<EntityUid> ImpritingTarget = new();

    [DataField]
    public float SearchRadius = 3f;

    [DataField]
    public bool Follow = true;
}
