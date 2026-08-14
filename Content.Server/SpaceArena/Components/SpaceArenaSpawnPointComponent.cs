using Content.Shared.SpaceArena;

namespace Content.Server.SpaceArena.Components;

[RegisterComponent]
public sealed partial class SpaceArenaSpawnPointComponent : Component
{
    [DataField(required: true)]
    public string Group = SpaceArenaSpawnGroups.Player;
}
