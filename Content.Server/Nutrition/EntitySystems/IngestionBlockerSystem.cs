using Content.Shared.Clothing;

namespace Content.Server.Nutrition.EntitySystems;

public sealed class IngestionBlockerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Components.IngestionBlockerComponent, ItemMaskToggledEvent>(OnBlockerMaskToggled);
    }

    private void OnBlockerMaskToggled(Entity<Components.IngestionBlockerComponent> ent, ref ItemMaskToggledEvent args)
    {
        ent.Comp.Enabled = !args.IsToggled;
    }
}
