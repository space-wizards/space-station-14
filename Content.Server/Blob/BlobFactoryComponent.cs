namespace Content.Server.Blob;

[RegisterComponent]
public sealed class BlobFactoryComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public float SpawnedCount = 0;

    [DataField("spawnLimit"), ViewVariables(VVAccess.ReadWrite)]
    public float SpawnLimit = 3;

    [DataField("spawnRate"), ViewVariables(VVAccess.ReadWrite)]
    public float SpawnRate = 10;

    [DataField("blobSporeId"), ViewVariables(VVAccess.ReadWrite)]
    public string Pod = "MobBlobPod";

    [DataField("blobbernautId"), ViewVariables(VVAccess.ReadWrite)]
    public string BlobbernautId = "MobBlobBlobbernaut";

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Blobbernaut = default!;

    public TimeSpan NextSpawn = TimeSpan.Zero;
}

public sealed class ProduceBlobbernautEvent : EntityEventArgs
{
}
