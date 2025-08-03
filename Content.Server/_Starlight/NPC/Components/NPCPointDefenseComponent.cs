using Content.Server.NPC.Systems;

namespace Content.Server.NPC.Components;

[RegisterComponent, Access(typeof(NPCPointDefenseSystem))]
public sealed partial class NPCPointDefenseComponent : Component
{
    [DataField]
    public string TargetKey = "Target";
}
