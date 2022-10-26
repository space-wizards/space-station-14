using Content.Shared.Materials;
using Robust.Client.GameObjects;

namespace Content.Client.Materials;

/// <summary>
/// This handles...
/// </summary>
public sealed class MaterialStorageSystem : SharedMaterialStorageSystem
{
    [Dependency] private readonly TransformSystem _transform = default!;

    public override bool TryInsertMaterialEntity(EntityUid user, EntityUid toInsert, EntityUid receiver, MaterialStorageComponent? component = null)
    {
        if (!base.TryInsertMaterialEntity(user, toInsert, receiver, component))
            return false;
        _transform.DetachParentToNull(Transform(toInsert));
        return true;
    }
}
