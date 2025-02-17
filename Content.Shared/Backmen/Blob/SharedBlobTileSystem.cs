using Content.Shared.Backmen.Blob.Components;
using Content.Shared.Verbs;

namespace Content.Shared.Backmen.Blob;

public abstract class SharedBlobTileSystem : EntitySystem
{
    protected EntityQuery<BlobObserverComponent> ObserverQuery;
    protected EntityQuery<BlobCoreComponent> CoreQuery;
    protected EntityQuery<TransformComponent> TransformQuery;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobTileComponent, GetVerbsEvent<AlternativeVerb>>(AddUpgradeVerb);
        SubscribeLocalEvent<BlobTileComponent, GetVerbsEvent<Verb>>(AddRemoveVerb);

        ObserverQuery = GetEntityQuery<BlobObserverComponent>();
        CoreQuery = GetEntityQuery<BlobCoreComponent>();
        TransformQuery = GetEntityQuery<TransformComponent>();
    }

    protected abstract void TryRemove(EntityUid target, EntityUid coreUid, BlobTileComponent tile, BlobCoreComponent core);

    protected abstract void TryUpgrade(EntityUid target, EntityUid user, EntityUid coreUid, BlobTileComponent tile, BlobCoreComponent core);

    private void AddRemoveVerb(EntityUid uid, BlobTileComponent component, GetVerbsEvent<Verb> args)
    {
        if (!ObserverQuery.TryGetComponent(args.User, out var ghostBlobComponent))
            return;

        if (ghostBlobComponent.Core == null ||
            !CoreQuery.TryGetComponent(ghostBlobComponent.Core.Value, out var blobCoreComponent))
            return;

        if (ghostBlobComponent.Core.Value != component.Core)
            return;

        if (TransformQuery.TryGetComponent(uid, out var transformComponent) && !transformComponent.Anchored)
            return;

        if (CoreQuery.HasComponent(uid))
            return;

        Verb verb = new()
        {
            Act = () => TryRemove(uid, ghostBlobComponent.Core.Value, component, blobCoreComponent),
            Text = Loc.GetString("blob-verb-remove-blob-tile"),
        };
        args.Verbs.Add(verb);
    }

    private void AddUpgradeVerb(EntityUid uid, BlobTileComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!ObserverQuery.TryGetComponent(args.User, out var ghostBlobComponent))
            return;

        if (ghostBlobComponent.Core == null ||
            !CoreQuery.TryGetComponent(ghostBlobComponent.Core.Value, out var blobCoreComponent))
            return;

        if (TransformQuery.TryGetComponent(uid, out var transformComponent) && !transformComponent.Anchored)
            return;

        var verbName = component.BlobTileType switch
        {
            BlobTileType.Normal => Loc.GetString("blob-verb-upgrade-to-strong"),
            BlobTileType.Strong => Loc.GetString("blob-verb-upgrade-to-reflective"),
            _ => Loc.GetString("blob-verb-upgrade")
        };

        AlternativeVerb verb = new()
        {
            Act = () => TryUpgrade(uid, args.User, ghostBlobComponent.Core.Value, component, blobCoreComponent),
            Text = verbName
        };
        args.Verbs.Add(verb);
    }
}
