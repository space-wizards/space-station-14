using Content.Shared.Nutrition.AnimalHusbandry;
using Robust.Client.GameObjects;

namespace Content.Client.Nutrition.EntitySystems;

/// <summary>
/// This handles visuals for <see cref="InfantComponent"/>
/// </summary>
public sealed class InfantSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<InfantComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<InfantComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, InfantComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        component.DefaultScale = sprite.Scale;
        sprite.Scale = component.VisualScale;
    }

    private void OnShutdown(EntityUid uid, InfantComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.Scale = component.DefaultScale;
    }
}
