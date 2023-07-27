using Robust.Shared.Audio;

[RegisterComponent]
public sealed class ZombieBlobComponent : Component
{
    public List<string> OldFations = new();

    public EntityUid BlobPodUid = default!;

    [DataField("greetSoundNotification")]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/zombie_start.ogg");
}
