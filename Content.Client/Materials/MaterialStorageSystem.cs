using Content.Shared.Materials;
using Robust.Client.GameObjects;

namespace Content.Client.Materials;

public sealed class MaterialStorageSystem : SharedMaterialStorageSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaterialStorageComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, MaterialStorageComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.Sprite.LayerMapTryGet(MaterialStorageVisualLayers.Inserting, out var layer))
            return;

        if (!_appearance.TryGetData<bool>(uid, MaterialStorageVisuals.Inserting, out var inserting, args.Component))
            return;

        if (inserting && TryComp<InsertingMaterialStorageComponent>(uid, out var insertingComp))
        {
            args.Sprite.LayerSetAnimationTime(layer, 0f);

            args.Sprite.LayerSetVisible(layer, true);
            if (insertingComp.MaterialColor != null)
                args.Sprite.LayerSetColor(layer, insertingComp.MaterialColor.Value);
        }
        else
        {
            args.Sprite.LayerSetVisible(layer, false);
        }
    }

    public override bool TryInsertMaterialEntity(EntityUid user, EntityUid toInsert, EntityUid receiver, MaterialStorageComponent? component = null)
    {
        if (!base.TryInsertMaterialEntity(user, toInsert, receiver, component))
            return false;
        _transform.DetachParentToNull(Transform(toInsert));
        return true;
    }
}

public enum MaterialStorageVisualLayers : byte
{
    Inserting
}
