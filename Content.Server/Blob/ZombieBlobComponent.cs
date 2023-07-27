using Robust.Shared.Audio;

namespace Content.Server.Blob;

[RegisterComponent]
public sealed class ZombieBlobComponent : Component
{
    public List<string> OldFactions = new();

    public EntityUid BlobPodUid = default!;

    [DataField("greetSoundNotification")]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/zombie_start.ogg");
}
