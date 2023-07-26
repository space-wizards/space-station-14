namespace Content.Server.Blob;

public sealed class BlobResourceSystem : EntitySystem
{
    [Dependency] private readonly BlobCoreSystem _blobCoreSystem = default!;

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
            _blobCoreSystem.ChangeBlobPoint(blobTileComponent.Core.Value, component.PointsPerPulsed);
        }
    }
}
