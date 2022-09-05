using Content.Shared.Materials;
using Robust.Client.GameObjects;

namespace Content.Client.Materials;

/// <summary>
/// This handles...
/// </summary>
public sealed class MaterialStorageSystem : SharedMaterialStorageSystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    protected override void OnFinishInsertMaterialEntity(EntityUid toInsert, MaterialStorageComponent component)
    {
        _transform.DetachParentToNull(Transform(toInsert));
    }
}
