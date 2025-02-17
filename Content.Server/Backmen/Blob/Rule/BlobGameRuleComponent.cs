using Robust.Shared.Audio;

namespace Content.Server.Backmen.Blob.Rule;

[RegisterComponent]
public sealed partial class BlobGameRuleComponent : Component
{
    public int TotalBlobs = 0;

    [DataField]
    public SoundSpecifier InitialInfectedSound = new SoundPathSpecifier("/Audio/_Backmen/Ambience/Antag/blob_start.ogg");
}
