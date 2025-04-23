using Content.Shared._Impstation.MobStateClothingVisuals;
using Content.Shared.Item;

namespace Content.Client._Impstation.ClothingMobStateVisuals;

public sealed partial class ClothingMobStateVisualsSystem : SharedMobStateClothingVisualsSystem
{
    [Dependency] private readonly SharedItemSystem _itemSys = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateClothingVisualsComponent, ClothingMobStateChangedEvent>(OnClothingMobStateChanged);
    }

    private void OnClothingMobStateChanged(Entity<MobStateClothingVisualsComponent> ent, ref ClothingMobStateChangedEvent args)
    {
        _itemSys.VisualsChanged(ent);
    }
}
