using Content.Shared.Vapor;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

public sealed class VaporVisualizerSystem : VisualizerSystem<VaporVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VaporVisualsComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, VaporVisualsComponent component, ComponentInit args)
    {
        component.VaporFlick = new Animation { Length = TimeSpan.FromSeconds(component.Delay) };
        var flick = new AnimationTrackSpriteFlick();
        component.VaporFlick.AnimationTracks.Add(flick);
        flick.LayerKey = VaporVisualLayers.Base;
        flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(component.State, 0f));
    }

    protected override void OnAppearanceChange(EntityUid uid, VaporVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Component.TryGetData<Color>(VaporVisuals.Color, out var color)
            && args.Sprite != null)
        {
            args.Sprite.Color = color;
        }

        if (args.Component.TryGetData<bool>(VaporVisuals.State, out var state)
            && TryComp(component.Owner, out AnimationPlayerComponent? animPlayer)
            && !animPlayer.HasRunningAnimation(component.AnimationKey))
        {
            animPlayer.Play(component.VaporFlick, component.AnimationKey);
        }
    }
}

public enum VaporVisualLayers : byte
{
    Base
}
