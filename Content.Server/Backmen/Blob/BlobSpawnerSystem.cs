using Content.Server.Backmen.Blob.Components;
using Content.Shared.Backmen.Blob;
using Content.Shared.Backmen.Blob.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Server.Backmen.Blob;

public sealed class BlobSpawnerSystem : EntitySystem
{
    [Dependency] private readonly BlobCoreSystem _blobCoreSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlobSpawnerComponent, PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(EntityUid uid, BlobSpawnerComponent component, PlayerAttachedEvent args)
    {
        var xform = Transform(uid);
        if (!HasComp<MapGridComponent>(xform.GridUid))
            return;

        var core = Spawn(component.CoreBlobPrototype, xform.Coordinates);

        if (!TryComp<BlobCoreComponent>(core, out var blobCoreComponent))
            return;

        if (_blobCoreSystem.CreateBlobObserver(core, args.Player.UserId, blobCoreComponent))
            QueueDel(uid);
    }
}
