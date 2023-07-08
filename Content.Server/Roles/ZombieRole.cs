using Content.Shared.Roles;

namespace Content.Server.Roles;

public sealed class ZombieRole : AntagonistRole
{
    public ZombieRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind, antagPrototype) { }
}
