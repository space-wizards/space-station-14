using Content.Shared.Foam;
using Content.Client.Chemistry.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client.Chemistry.EntitySystems;

public sealed class FoamVisualizerSystem : VisualizerSystem<FoamVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoamVisualsComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, FoamVisualsComponent component, ComponentInit args)
    {
        component.FoamDissolve = new Animation { Length = TimeSpan.FromSeconds(component.Delay) };
        var flick = new AnimationTrackSpriteFlick();
        component.FoamDissolve.AnimationTracks.Add(flick);
        flick.LayerKey = FoamVisualLayers.Base;
        flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(component.State, 0f));
    }

    protected override void OnAppearanceChange(EntityUid uid, FoamVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Component.TryGetData<bool>(FoamVisuals.State, out var state)
            && TryComp(component.Owner, out AnimationPlayerComponent? animPlayer)
            && !animPlayer.HasRunningAnimation(component.AnimationKey))
        {
            animPlayer.Play(component.FoamDissolve, component.AnimationKey);
        }

        if (args.Component.TryGetData<Color>(FoamVisuals.Color, out var color)
            && args.Sprite != null)
        {
            args.Sprite.Color = color;
        }
    }
}

public enum FoamVisualLayers : byte
{
    Base
}
