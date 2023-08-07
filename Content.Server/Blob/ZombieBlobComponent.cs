using Robust.Shared.Audio;

namespace Content.Server.Blob;

[RegisterComponent]
public sealed class ZombieBlobComponent : Component
{
    public List<string> OldFactions = new();

    public EntityUid BlobPodUid = default!;

    public float? OldColdDamageThreshold = null;

    [ViewVariables]
    public Dictionary<string, int> DisabledFixtureMasks { get; } = new();

    [DataField("greetSoundNotification")]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/zombie_start.ogg");
}
