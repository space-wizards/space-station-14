using Content.Shared.Animals;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Shearing;
using Robust.Client.GameObjects;

namespace Content.Client.Animals;

public sealed partial class ShearableVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShearableComponent, ShearableLayerUpdateEvent>(UpdateShearableLayer);
    }

    private void UpdateShearableLayer(Entity<ShearableComponent> ent, ref ShearableLayerUpdateEvent args)
    {
        {
            
        }
    }

}