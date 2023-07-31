using Content.Shared.Roles;

namespace Content.Server.Roles;
/// <summary>
/// Used for assigning the Revolutionary role.
/// </summary>
public sealed class RevolutionaryRole : AntagonistRole
{
    public RevolutionaryRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind, antagPrototype) { }
}
