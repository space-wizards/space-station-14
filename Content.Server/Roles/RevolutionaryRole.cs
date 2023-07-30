using Content.Shared.Roles;

namespace Content.Server.Roles;

public sealed class RevolutionaryRole : AntagonistRole
{
    public RevolutionaryRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind, antagPrototype) { }
}
