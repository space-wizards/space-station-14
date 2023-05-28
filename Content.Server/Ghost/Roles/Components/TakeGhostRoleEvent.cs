using Robust.Server.Player;

namespace Content.Server.Ghost.Roles.Components;

[ByRefEvent]
public record struct TakeGhostRoleEvent(IPlayerSession Player, string UserId)
{
    public bool TookRole { get; set; }
}
