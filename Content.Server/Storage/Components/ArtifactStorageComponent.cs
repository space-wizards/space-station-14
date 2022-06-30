using Content.Server.Xenoarchaeology.XenoArtifacts;

namespace Content.Server.Storage.Components;

[RegisterComponent]
public sealed class ArtifactStorageComponent : Component
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    /*
    public override bool CanFit(EntityUid entity)
    {
        return _entMan.HasComponent<ArtifactComponent>(entity);
    }*/
}
