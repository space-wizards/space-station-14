using Content.Shared.Roles;

namespace Content.Server.Roles;

public sealed class BlobRole : AntagonistRole
{
    public BlobRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind, antagPrototype) { }
}
