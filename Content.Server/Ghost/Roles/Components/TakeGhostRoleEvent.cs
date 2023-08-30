using Robust.Server.Player;

namespace Content.Server.Ghost.Roles.Components;

[ByRefEvent]
public record struct TakeGhostRoleEvent(IPlayerSession Player)
{
    public bool TookRole { get; set; }
}
