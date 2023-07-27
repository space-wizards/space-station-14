[RegisterComponent]
public sealed class ZombieBlobComponent : Component
{
    public List<string> OldFations = new();

    public EntityUid BlobPodUid = default!;
}
