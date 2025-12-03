using Content.Shared.Eye.Blinking;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using System.ComponentModel.Design;

namespace Content.Client.Eye.Blinking;
public sealed partial class EyeBlinkingSystem : SharedEyeBlinkingSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly ITimerManager _timer = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EyeBlinkingComponent, AppearanceChangeEvent>(AppearanceChangeEventHandler);
        SubscribeLocalEvent<EyeBlinkingComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<EyeBlinkingComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var spriteComponent))
            return;

        if (!_sprite.TryGetLayer(ent.Owner, HumanoidVisualLayers.Eyes, out var layer, false))
            return;

        if (_appearance.TryGetData(ent.Owner, EyeBlinkingVisuals.EyesClosed, out var stateObj) && stateObj is bool state)
            ChangeEyeState(ent, state);
    }

    private void AppearanceChangeEventHandler(Entity<EyeBlinkingComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!args.AppearanceData.TryGetValue(EyeBlinkingVisuals.EyesClosed, out var closed))
            return;

        if (!(closed is bool state))
            return;

        if (ent.Comp.EyesClosed == state)
            return;

        Logger.Info($"Eye closed cganged to {state} for entity {ent.Owner}");

        ent.Comp.EyesClosed = state;
        Dirty(ent);

        ChangeEyeState(ent, state);
    }

    private void ChangeEyeState(Entity<EyeBlinkingComponent> ent, bool eyeClsoed)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent.Owner, out var humanoid))
            return;
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;
        if (!_sprite.LayerMapTryGet(ent.Owner, HumanoidVisualLayers.Eyes, out var layer, false))
            return;

        var blinkFade = ent.Comp.BlinkSkinColorMultiplier;
        var blinkColor = new Color(
            humanoid.SkinColor.R * blinkFade,
            humanoid.SkinColor.G * blinkFade,
            humanoid.SkinColor.B * blinkFade);
        var eyeColor = humanoid.EyeColor;
        _sprite.LayerSetColor((ent.Owner, sprite), HumanoidVisualLayers.Eyes, eyeClsoed ? blinkColor : eyeColor);
    }

    public override void Blink(Entity<EyeBlinkingComponent> ent)
    {
        if (!ent.Owner.IsValid())
            return;

        base.Blink(ent);

        if (ent.Comp.EyesClosed)
            return;

        if (ent.Comp.BlinkInProgress)
            return;

        ent.Comp.BlinkInProgress = true;

        ChangeEyeState(ent, true);

        Timer timer = new Timer((int)ent.Comp.BlinkDuration.TotalMilliseconds, false, () => OpenEye(ent));

        _timer.AddTimer(timer);
    }

    private void OpenEye(Entity<EyeBlinkingComponent> ent)
    {
        ent.Comp.BlinkInProgress = false;

        if (ent.Comp.EyesClosed)
            return;


        ChangeEyeState(ent, false);
    }
}
