using Content.Shared.Eye.Blinking;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;

namespace Content.Client.Eye.Blinking;
public sealed partial class EyeBlinkingSystem : SharedEyeBlinkingSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EyeBlinkingComponent, AppearanceChangeEvent>(OnAppearance);
        SubscribeLocalEvent<EyeBlinkingComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<EyeBlinkingComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var spriteComponent))
            return;

        if (_appearance.TryGetData(ent.Owner, SichEyeBlinkingVisuals.Blinking, out var stateObj) && stateObj is bool state)
        {
            ChangeEyeState(ent, spriteComponent, state);
        }
    }

    private void OnAppearance(Entity<EyeBlinkingComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var spriteComponent))
            return;

        if (args.AppearanceData.TryGetValue(SichEyeBlinkingVisuals.Blinking, out var stateObj) && stateObj is bool state)
        {
            ChangeEyeState(ent, spriteComponent, state);
        }
    }

    private void ChangeEyeState(Entity<EyeBlinkingComponent> ent, SpriteComponent sprite, bool isBlinking)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent.Owner, out var humanoid)) return;
        var blinkFade = ent.Comp.BlinkSkinColorMultiplier;
        var blinkColor = new Color(
            humanoid.SkinColor.R * blinkFade,
            humanoid.SkinColor.G * blinkFade,
            humanoid.SkinColor.B * blinkFade);
        sprite[_sprite.LayerMapReserve((ent.Owner, sprite), HumanoidVisualLayers.Eyes)].Color = isBlinking ? blinkColor : humanoid.EyeColor;
    }
}
