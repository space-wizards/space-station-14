using Content.Shared.Popups;

namespace Content.Server.Blob;

public sealed class BlobResourceSystem : EntitySystem
{
    [Dependency] private readonly BlobCoreSystem _blobCoreSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobResourceComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BlobResourceComponent, BlobTileGetPulseEvent>(OnPulsed);
    }


    private void OnStartup(EntityUid uid, BlobResourceComponent component, ComponentStartup args)
    {

    }

    private void OnPulsed(EntityUid uid, BlobResourceComponent component, BlobTileGetPulseEvent args)
    {
        if (TryComp<BlobTileComponent>(uid, out var blobTileComponent) && blobTileComponent.Core != null)
        {
            if (TryComp<BlobCoreComponent>(blobTileComponent.Core, out var blobCoreComponent) && blobCoreComponent.Observer != null)
            {
                _popup.PopupEntity(Loc.GetString("blob-get-resource", ("point", component.PointsPerPulsed)),
                    uid,
                    blobCoreComponent.Observer.Value,
                    PopupType.Large);
            }
            _blobCoreSystem.ChangeBlobPoint(blobTileComponent.Core.Value, component.PointsPerPulsed);
        }
    }
}
