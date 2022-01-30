using Content.Server.Sprite.Components;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Sprite;

public sealed class RandomSpriteSystem: EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RandomSpriteColorComponent, ComponentStartup>(OnSpriteColorStartup);
        SubscribeLocalEvent<RandomSpriteColorComponent, MapInitEvent>(OnSpriteColorMapInit);

        SubscribeLocalEvent<RandomSpriteStateComponent, MapInitEvent>(OnSpriteStateMapInit);
    }

    private void OnSpriteColorStartup(EntityUid uid, RandomSpriteColorComponent component, ComponentStartup args)
    {
        UpdateColor(component);
    }

    private void OnSpriteColorMapInit(EntityUid uid, RandomSpriteColorComponent component, MapInitEvent args)
    {
        component.SelectedColor = _random.Pick(component.Colors.Keys);
        UpdateColor(component);
    }

    private void OnSpriteStateMapInit(EntityUid uid, RandomSpriteStateComponent component, MapInitEvent args)
    {
        if (component.SpriteStates == null) return;
        if (!TryComp<SpriteComponent>(uid, out var spriteComponent)) return;
        spriteComponent.LayerSetState(component.SpriteLayer, _random.Pick(component.SpriteStates));
    }

    private void UpdateColor(RandomSpriteColorComponent component)
    {
        if (!TryComp<SpriteComponent>(component.Owner, out var spriteComponent) || component.SelectedColor == null) return;

        spriteComponent.LayerSetState(0, component.BaseState);
        spriteComponent.LayerSetColor(0, component.Colors[component.SelectedColor]);
    }
}
