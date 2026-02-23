using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Antag;

[Serializable]
public record struct AntagData
{
    public List<EntProtoId>? MindRoles;
    public ComponentRegistry AddAntagComponents;
    public ComponentRegistry PlayerComponents;
    public HashSet<ProtoId<NpcFactionPrototype>> AddFactions;
    public HashSet<ProtoId<NpcFactionPrototype>> RemoveFactions;
    public EntityUid AntagEntity;
}
