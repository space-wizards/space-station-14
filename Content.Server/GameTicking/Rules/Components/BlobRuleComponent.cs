using Content.Server.Blob;
using Content.Server.Roles;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(BlobRuleSystem), typeof(BlobCoreSystem))]
public sealed class BlobRuleComponent : Component
{
    public List<BlobRole> Blobs = new();

}
