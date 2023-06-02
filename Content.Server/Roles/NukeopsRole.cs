using Content.Shared.Roles;

namespace Content.Server.Roles;

public sealed class NukeopsRole : AntagonistRole
{
    public NukeopsRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind, antagPrototype) { }
}
