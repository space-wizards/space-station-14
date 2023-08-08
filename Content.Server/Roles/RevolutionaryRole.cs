using Content.Shared.Roles;

namespace Content.Server.Roles;
/// <summary>
/// Role used for assigning Revolutionaries and is assigned when the Revolutionary gamerule starts.
/// </summary>
public sealed class RevolutionaryRole : AntagonistRole
{
    public RevolutionaryRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind, antagPrototype) { }
}
