using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Server.SpaceArena.Components;

[RegisterComponent, Access(typeof(SpaceArenaLobbyTerminalSystem))]
public sealed partial class SpaceArenaLobbyTerminalComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> Modes = [];

    [DataField(required: true)]
    public List<ProtoId<GameMapPrototype>> Arenas = [];
}
