using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Light.Components;
using ItemTogglePointLightComponent = Content.Shared.Light.Components.ItemTogglePointLightComponent;

namespace Content.Shared.Light.EntitySystems;

/// <summary>
/// Implements the behavior of <see cref="ItemTogglePointLightComponent"/>, causing <see cref="ItemToggledEvent"/>s to
/// enable and disable lights on the entity.
/// </summary>
public sealed class ItemTogglePointLightSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SharedHandheldLightSystem _handheldLight = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ItemTogglePointLightComponent, ItemToggledEvent>(OnLightToggled);
    }

    private void OnLightToggled(Entity<ItemTogglePointLightComponent> ent, ref ItemToggledEvent args)
    {
        if (!_light.TryGetLight(ent.Owner, out var light))
            return;

        _light.SetEnabled(ent.Owner, args.Activated, comp: light);
        if (TryComp<HandheldLightComponent>(ent.Owner, out var handheldLight))
        {
            _handheldLight.SetActivated(ent.Owner, args.Activated, handheldLight);
        }
    }
}
