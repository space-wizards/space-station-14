using Content.Shared.Materials;
using Robust.Client.GameObjects;

namespace Content.Client.Materials;

public sealed class MaterialStorageSystem : SharedMaterialStorageSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaterialStorageComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, MaterialStorageComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_sprite.LayerMapTryGet((uid, args.Sprite), MaterialStorageVisualLayers.Inserting, out var layer, false))
            return;

        if (!_appearance.TryGetData<bool>(uid, MaterialStorageVisuals.Inserting, out var inserting, args.Component))
            return;

        if (inserting && TryComp<InsertingMaterialStorageComponent>(uid, out var insertingComp))
        {
            _sprite.LayerSetAnimationTime((uid, args.Sprite), layer, 0f);

            _sprite.LayerSetVisible((uid, args.Sprite), layer, true);
            if (insertingComp.MaterialColor != null)
                _sprite.LayerSetColor((uid, args.Sprite), layer, insertingComp.MaterialColor.Value);
        }
        else
        {
            _sprite.LayerSetVisible((uid, args.Sprite), layer, false);
        }
    }

    public override bool TryInsertMaterialEntity(EntityUid user,
        EntityUid toInsert,
        EntityUid receiver,
        MaterialStorageComponent? storage = null,
        MaterialComponent? material = null,
        PhysicalCompositionComponent? composition = null)
    {
        if (!base.TryInsertMaterialEntity(user, toInsert, receiver, storage, material, composition))
            return false;
        _transform.DetachEntity(toInsert, Transform(toInsert));
        return true;
    }
}

public enum MaterialStorageVisualLayers : byte
{
    Inserting
}
