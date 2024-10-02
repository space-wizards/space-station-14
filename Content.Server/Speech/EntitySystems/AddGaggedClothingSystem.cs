using Content.Server.Speech.Components;
using Content.Shared.Clothing;

namespace Content.Server.Speech.EntitySystems;

/// <summary>
/// Applies <see cref="GaggedComponent"/> when clothing with <see cref="AddGaggedClothingComponent"/> is equipped
/// and removes it when it's unequipped.
/// </summary>
public sealed class AddGaggedClothingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddGaggedClothingComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<AddGaggedClothingComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(EntityUid uid, AddGaggedClothingComponent component, ref ClothingGotEquippedEvent args)
    {
        AddComp<GaggedComponent>(args.Wearer);
    }

    private void OnGotUnequipped(EntityUid uid, AddGaggedClothingComponent component, ref ClothingGotUnequippedEvent args)
    {
        if (EntityManager.HasComponent<GaggedComponent>(args.Wearer))
        {
            EntityManager.RemoveComponent<GaggedComponent>(args.Wearer);
        }
    }
}
