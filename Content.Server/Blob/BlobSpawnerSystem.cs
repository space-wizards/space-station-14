using Content.Shared.Blob;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Blob
{
    public sealed class BlobSpawnerSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly BlobCoreSystem _blobCoreSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BlobSpawnerComponent, PlayerAttachedEvent>(OnPlayerAttached);
        }

        private void OnPlayerAttached(EntityUid uid, BlobSpawnerComponent component, PlayerAttachedEvent args)
        {
            var xform = Transform(uid);
            if (!_mapManager.TryGetGrid(xform.GridUid, out var map))
                return;

            var core = Spawn(component.CoreBlobPrototype, xform.Coordinates);

            if (!TryComp<BlobCoreComponent>(core, out var blobCoreComponent))
                return;

            if (_blobCoreSystem.CreateBlobObserver(core, args.Player.UserId, blobCoreComponent))
                QueueDel(uid);
        }
    }
}
