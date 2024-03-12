using Content.Server.NPC.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.NPC.Components;
/// <summary>
/// A component that makes the entity friendly to the nearest creature it sees on init.
/// </summary>
[RegisterComponent]
[Access(typeof(NPCDuckingSyndromeSystem))]
public sealed partial class NPCDuckingSyndromeComponent : Component
{
    [DataField]
    public HashSet<EntityUid> SyndromeTarget;

    [DataField]
    public float SearchRadius = 10f;
}
