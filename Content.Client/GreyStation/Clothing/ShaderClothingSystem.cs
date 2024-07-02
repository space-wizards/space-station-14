using Content.Shared.Clothing;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.GreyStation.Clothing;

/// <summary>
/// Applies the overlay when shader clothing is worn.
/// </summary>
public sealed class ShaderClothingSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private ShaderClothingOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();

        SubscribeLocalEvent<ShaderClothingComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ShaderClothingComponent, ClothingGotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<ShaderClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        ent.Comp.Instance ??= _proto.Index(ent.Comp.Shader).InstanceUnique();
        _overlay.Shader = ent.Comp.Instance;
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnUnequipped(Entity<ShaderClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }
}
