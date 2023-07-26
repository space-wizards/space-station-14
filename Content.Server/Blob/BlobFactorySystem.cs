using Content.Shared.Destructible;
using Robust.Shared.Timing;

namespace Content.Server.Blob;

public sealed class BlobFactorySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobFactoryComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BlobFactoryComponent, BlobTileGetPulseEvent>(OnPulsed);
        SubscribeLocalEvent<BlobFactoryComponent, ProduceBlobbernautEvent>(OnProduceBlobbernaut);
        SubscribeLocalEvent<BlobFactoryComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnStartup(EntityUid uid, BlobFactoryComponent observerComponent, ComponentStartup args)
    {

    }

    private void OnDestruction(EntityUid uid, BlobFactoryComponent component, DestructionEventArgs args)
    {
        if (TryComp<BlobbernautComponent>(component.Blobbernaut, out var blobbernautComponent))
        {
            blobbernautComponent.Factory = null;
        }
    }

    private void OnProduceBlobbernaut(EntityUid uid, BlobFactoryComponent component, ProduceBlobbernautEvent args)
    {
        if (component.Blobbernaut != null)
            return;

        var xform = Transform(uid);

        var blobbernaut = Spawn(component.BlobbernautId, xform.Coordinates);

        component.Blobbernaut = blobbernaut;
        if (TryComp<BlobbernautComponent>(blobbernaut, out var blobbernautComponent))
        {
            blobbernautComponent.Factory = uid;
        }
    }

    private void OnPulsed(EntityUid uid, BlobFactoryComponent component, BlobTileGetPulseEvent args)
    {
        if (!TryComp<BlobTileComponent>(uid, out var blobTileComponent) || blobTileComponent.Core == null)
            return;
        if (component.SpawnedCount >= component.SpawnLimit)
            return;

        if (_gameTiming.CurTime < component.NextSpawn)
            return;

        var xform = Transform(uid);
        Spawn(component.Pod, xform.Coordinates);
        component.SpawnedCount += 1;
        component.NextSpawn = _gameTiming.CurTime + TimeSpan.FromSeconds(component.SpawnRate);
    }

}
