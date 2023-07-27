using Content.Server.Roles;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(BlobRuleSystem))]
public sealed class BlobRuleComponent : Component
{
    public List<BlobRole> Blobs = new();

}
