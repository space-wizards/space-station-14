using Content.Shared.Eye.Blinking;
using Content.Shared.Humanoid;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Animations;
using Robust.Shared.Timing;
using System.Numerics;

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

        


        //if (_appearance.TryGetData(ent.Owner, EyeBlinkingVisuals.EyesClosed, out var stateObj) && stateObj is bool state)
        //{
        //    ChangeEyeState(ent, state);
        //}
    }

    private void AppearanceChangeEventHandler(Entity<EyeBlinkingComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData<bool>(ent.Owner, EyeBlinkingVisuals.EyesClosed, out var closed))
            return;

        if (!_sprite.LayerMapTryGet(ent.Owner, HumanoidVisualLayers.Eyes, out var layer, false))
            return;

        //ChangeEyeState(ent, closed);
    }

    private void ChangeEyeState(Entity<EyeBlinkingComponent> ent, bool eyeClsoed)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent.Owner, out var humanoid))
            return;
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
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
        base.Blink(ent);
        if (ent.Comp.BlinkInProgress)
            return;

        if (!_sprite.TryGetLayer(ent.Owner, HumanoidVisualLayers.Eyes, out var eyes, false))
            return;

        ent.Comp.BlinkInProgress = true;

        ChangeEyeState(ent, true);

        Timer timer = new Timer((int)ent.Comp.BlinkDuration.TotalMilliseconds, false, () => OpenEye(ent));

        _timer.AddTimer(timer);
    }

    private void OpenEye(Entity<EyeBlinkingComponent> ent)
    {
        if (!ent.Owner.IsValid())
            return;
        ent.Comp.BlinkInProgress = false;
        if (_appearance.TryGetData(ent.Owner, EyeBlinkingVisuals.EyesClosed, out var stateObj) && stateObj is bool state)
        {
            if(state == false)
                ChangeEyeState(ent, state);
        }
        else
        {
            ChangeEyeState(ent, false);
        }
    }
}

public enum EyelidsVisuals : byte
{
    Eyelids,
}
