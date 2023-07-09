using Content.Client.Light.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.Light.EntitySystems;

public sealed class LightFadeSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _player = default!;

    private const string FadeTrack = "light-fade";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LightFadeComponent, ComponentStartup>(OnFadeStartup);
    }

    private void OnFadeStartup(EntityUid uid, LightFadeComponent component, ComponentStartup args)
    {
        if (!TryComp<PointLightComponent>(uid, out var light))
            return;

        var animation = new Animation()
        {
            Length = TimeSpan.FromSeconds(component.Duration),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    Property = nameof(PointLightComponent.Energy),
                    ComponentType = typeof(PointLightComponent),
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(light.Energy, 0f),
                        new AnimationTrackProperty.KeyFrame(0f, component.Duration)
                    }
                }
            }
        };

        _player.Play(uid, animation, FadeTrack);
    }
}
