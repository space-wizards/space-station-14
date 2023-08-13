using System.Linq;
using Content.Client.Light.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Random;

namespace Content.Client.Light.EntitySystems;

public sealed class LightBehaviorSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AnimationPlayerSystem _player = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LightBehaviourComponent, ComponentStartup>(OnLightStartup);
        SubscribeLocalEvent<LightBehaviourComponent, AnimationCompletedEvent>(OnBehaviorAnimationCompleted);
    }

    private void OnBehaviorAnimationCompleted(EntityUid uid, LightBehaviourComponent component, AnimationCompletedEvent args)
    {
        var container = component.Animations.FirstOrDefault(x => x.FullKey == args.Key);

        if (container == null)
        {
            return;
        }

        if (container.LightBehaviour.IsLooped)
        {
            container.LightBehaviour.UpdatePlaybackValues(container.Animation);
            _player.Play(uid, container.Animation, container.FullKey);
        }
    }

    private void OnLightStartup(EntityUid uid, LightBehaviourComponent component, ComponentStartup args)
    {
        // TODO: Do NOT ensure component here. And use eventbus events instead...
        EnsureComp<AnimationPlayerComponent>(uid);

        foreach (var container in component.Animations)
        {
            container.LightBehaviour.Initialize(uid, _random, EntityManager);
        }

        // we need to initialize all behaviours before starting any
        foreach (var container in component.Animations)
        {
            if (container.LightBehaviour.Enabled)
            {
                component.StartLightBehaviour(container.LightBehaviour.ID);
            }
        }
    }
}
