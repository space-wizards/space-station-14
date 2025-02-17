using Content.Server.Backmen.Blob.Components;
using Content.Shared.Backmen.Blob;
using Content.Shared.Backmen.Blob.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;

namespace Content.Server.Backmen.Blob;

public sealed class BlobResourceSystem : EntitySystem
{
    [Dependency] private readonly BlobCoreSystem _blobCoreSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobResourceComponent, BlobTileGetPulseEvent>(OnPulsed);
    }

    private void OnPulsed(EntityUid uid, BlobResourceComponent component, BlobTileGetPulseEvent args)
    {
        if (!TryComp<BlobTileComponent>(uid, out var blobTileComponent) || blobTileComponent.Core == null)
            return;
        if (!TryComp<BlobCoreComponent>(blobTileComponent.Core, out var blobCoreComponent) ||
            blobCoreComponent.Observer == null)
            return;
        _popup.PopupEntity(Loc.GetString("blob-get-resource", ("point", component.PointsPerPulsed)),
            uid,
            blobCoreComponent.Observer.Value,
            PopupType.Large);

        var points = component.PointsPerPulsed;

        if (blobCoreComponent.CurrentChem == BlobChemType.RegenerativeMateria)
        {
            points += 1;
        }

        _blobCoreSystem.ChangeBlobPoint(blobTileComponent.Core.Value, points);
    }
}
