using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Antag;

[Serializable]
public record struct AntagData
{
    public List<EntProtoId>? MindRoles;
    public ComponentRegistry AntagComponents;
    public ComponentRegistry PlayerComponents;
    public HashSet<ProtoId<NpcFactionPrototype>> Factions;
    public EntityUid AntagEntity;
}
