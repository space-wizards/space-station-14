using Content.Server.Storage.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts;

namespace Content.Server.Storage.EntitySystems;

public sealed class ArtifactStorageSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactStorageComponent, StorageBeforeCloseEvent>(OnBeforeClose);
    }

    private void OnBeforeClose(EntityUid uid, ArtifactStorageComponent component, StorageBeforeCloseEvent args)
    {
        foreach (var ent in args.Contents)
        {
            if (HasComp<ArtifactComponent>(ent))
                args.ContentsWhitelist.Add(ent);
        }
    }
}
