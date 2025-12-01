using Content.Shared.Eye.Blinking;
using Content.Shared.Humanoid;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Animations;
using System.Numerics;

namespace Content.Client.Eye.Blinking;
public sealed partial class EyeBlinkingSystem : SharedEyeBlinkingSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    private const string AnimationKey = "anim-blink";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EyeBlinkingComponent, AppearanceChangeEvent>(AppearanceChangeEventHandler);
        SubscribeLocalEvent<EyeBlinkingComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<EyeBlinkingComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent.Owner, out var humanoid))
            return;

        if (!TryComp<SpriteComponent>(ent.Owner, out var spriteComponent))
            return;

        if (!_sprite.TryGetLayer(ent.Owner, HumanoidVisualLayers.Eyes, out var layer, false))
            return;

        var layerIndex = _sprite.LayerMapReserve((ent.Owner, spriteComponent), HumanoidVisualLayers.Eyes);
        _sprite.LayerMapAdd((ent.Owner, spriteComponent), EyelidsVisuals.Eyelids, layerIndex);

        var eyelidLayerIndex = _sprite.LayerMapReserve((ent.Owner, spriteComponent), EyelidsVisuals.Eyelids);
        _sprite.LayerSetRsiState((ent.Owner, spriteComponent), eyelidLayerIndex, layer.State);

        var blinkFade = ent.Comp.BlinkSkinColorMultiplier;
        var blinkColor = new Color(
            humanoid.SkinColor.R * blinkFade,
            humanoid.SkinColor.G * blinkFade,
            humanoid.SkinColor.B * blinkFade);
        _sprite.LayerSetColor((ent.Owner, spriteComponent), layerIndex, layer.Color);


        if (_appearance.TryGetData(ent.Owner, EyeBlinkingVisuals.EyesClosed, out var stateObj) && stateObj is bool state)
        {
            ChangeEyeState(ent, state);
        }
    }

    private void AppearanceChangeEventHandler(Entity<EyeBlinkingComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData<bool>(ent.Owner, EyeBlinkingVisuals.EyesClosed, out var closed))
            return;

        if (!_sprite.LayerMapTryGet(ent.Owner, EyelidsVisuals.Eyelids, out var layer, false))
            return;

        ChangeEyeState(ent, closed);
    }

    private void ChangeEyeState(Entity<EyeBlinkingComponent> ent, bool eyeClsoed)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite)) return;

        sprite[_sprite.LayerMapReserve((ent.Owner, sprite), EyelidsVisuals.Eyelids)].Visible = eyeClsoed;
    }

    public override void Blink(Entity<EyeBlinkingComponent> ent)
    {
        base.Blink(ent);
        if (_animationPlayer.HasRunningAnimation(ent.Owner, AnimationKey))
            return;

        if (!_sprite.TryGetLayer(ent.Owner, EyelidsVisuals.Eyelids, out var layer, false))
            return;

        var animation = new Animation
        {
            Length = ent.Comp.BlinkDuration,
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = EyelidsVisuals.Eyelids,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId("no_eyes"), 0f),
                        new AnimationTrackSpriteFlick.KeyFrame(layer.State, (float)ent.Comp.BlinkDuration.TotalSeconds),
                    }
                }
            }
        };

        _animationPlayer.Play(ent.Owner, animation, AnimationKey);
    }
}

public enum EyelidsVisuals : byte
{
    Eyelids,
}
