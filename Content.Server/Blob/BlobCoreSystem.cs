using Content.Shared.Alert;
using Content.Shared.Blob;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Map;

namespace Content.Server.Blob;

public sealed class BlobCoreSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobCoreComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BlobCoreComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnStartup(EntityUid uid, BlobCoreComponent observerComponent, ComponentStartup args)
    {
        ChangeBlobPoint(uid, 0, observerComponent);

        if (TryComp<BlobTileComponent>(uid, out var blobTileComponent))
        {
            blobTileComponent.Core = uid;
        }
    }

    private void OnDestruction(EntityUid uid, BlobCoreComponent component, DestructionEventArgs args)
    {
        if (component.Observer != null)
        {
            QueueDel(component.Observer.Value);
        }

        foreach (var blobTile in component.BlobTiles)
        {
            if (!TryComp<BlobTileComponent>(blobTile, out var blobTileComponent))
                continue;
            blobTileComponent.Core = null;

            blobTileComponent.State = BlobTileState.Dead;
            Dirty(blobTileComponent);
        }
    }

    public bool TransformBlobTile(EntityUid? oldTileUid, EntityUid coreTileUid, string newBlobTileProto, EntityCoordinates coordinates, BlobCoreComponent? blobCore = null)
    {
        if (!Resolve(coreTileUid, ref blobCore))
            return false;
        if (oldTileUid != null)
        {
            QueueDel(oldTileUid.Value);
            blobCore.BlobTiles.Remove(oldTileUid.Value);
        }
        var resourceBlob = EntityManager.SpawnEntity(newBlobTileProto, coordinates);
        if (TryComp<BlobTileComponent>(resourceBlob, out var blobTileComponent))
            blobTileComponent.Core = coreTileUid;
        blobCore.BlobTiles.Add(resourceBlob);
        return true;
    }

    public bool ChangeBlobPoint(EntityUid uid, FixedPoint2 amount, BlobCoreComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        component.Points += amount;

        if (component.Observer != null)
            _alerts.ShowAlert(component.Observer.Value, AlertType.BlobResource, (short) Math.Clamp(Math.Round(component.Points.Float() / 10f), 0, 16));

        return true;
    }

    public bool TryUseAbility(EntityUid uid, EntityUid coreUid, BlobCoreComponent component, FixedPoint2 abilityCost)
    {
        if (component.Points < abilityCost)
        {
            _popup.PopupEntity(Loc.GetString("blob-not-enough-resources"), uid, uid);
            return false;
        }

        ChangeBlobPoint(coreUid, -abilityCost, component);

        return true;
    }
}
