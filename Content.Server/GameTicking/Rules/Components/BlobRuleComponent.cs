using Content.Server.Blob;
using Content.Server.Roles;
using Robust.Shared.Audio;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(BlobRuleSystem), typeof(BlobCoreSystem))]
public sealed class BlobRuleComponent : Component
{
    public List<BlobRole> Blobs = new();

    public BlobStage Stage = BlobStage.Default;

    [DataField("alertAodio")]
    public SoundSpecifier? AlertAudio = new SoundPathSpecifier("/Audio/Announcements/attention.ogg");
}


public enum BlobStage : byte
{
    Default,
    Begin,
    Medium,
    Critical,
    TheEnd
}
