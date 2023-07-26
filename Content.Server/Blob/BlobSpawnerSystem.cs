using Content.Server.Mind;
using Content.Server.Players;
using Content.Shared.Blob;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;

namespace Content.Server.Blob
{
    public sealed class BlobSpawnerSystem : EntitySystem
    {
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly MindSystem _mindSystem = default!;

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
            var observer = Spawn(component.ObserverBlobPrototype, xform.Coordinates);

            if (TryComp<BlobCoreComponent>(core, out var blobCoreComponent))
                blobCoreComponent.Observer = observer;

            if (TryComp<BlobObserverComponent>(observer, out var blobObserverComponent))
                blobObserverComponent.Core = core;

            _audioSystem.PlayPvs(component.SpawnSoundPath, uid, AudioParams.Default.WithVolume(-6f));

            var mind = args.Player.ContentData()?.Mind;
            if (mind == null)
                return;

            _mindSystem.TransferTo(mind, observer, ghostCheckOverride: true);

            QueueDel(uid);
        }
    }
}
