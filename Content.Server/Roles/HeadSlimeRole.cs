using Content.Shared.Roles;

namespace Content.Server.Roles;

public sealed class HeadSlimeRole : AntagonistRole
{
    public HeadSlimeRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind, antagPrototype) { }
}
