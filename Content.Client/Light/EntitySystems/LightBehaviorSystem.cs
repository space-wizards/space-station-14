using System.Linq;
using Content.Client.Light.Components;
using Robust.Client.GameObjects;
using Robust.Client.Animations;
using Robust.Shared.Random;
using Robust.Shared.Animations;

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
        if (!args.Finished)
            return;

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

    private void OnLightStartup(Entity<LightBehaviourComponent> entity, ref ComponentStartup args)
    {
        // TODO: Do NOT ensure component here. And use eventbus events instead...
        EnsureComp<AnimationPlayerComponent>(entity);

        foreach (var container in entity.Comp.Animations)
        {
            container.LightBehaviour.Initialize(entity, _random, EntityManager);
        }

        // we need to initialize all behaviours before starting any
        foreach (var container in entity.Comp.Animations)
        {
            if (container.LightBehaviour.Enabled)
            {
                StartLightBehaviour(entity, container.LightBehaviour.ID);
            }
        }
    }

    /// <summary>
    /// If we disable all the light behaviours we want to be able to revert the light to its original state.
    /// </summary>
    private void CopyLightSettings(Entity<LightBehaviourComponent> entity, string property)
    {
        if (EntityManager.TryGetComponent(entity, out PointLightComponent? light))
        {
            var propertyValue = AnimationHelper.GetAnimatableProperty(light, property);
            if (propertyValue != null)
            {
                entity.Comp.OriginalPropertyValues.Add(property, propertyValue);
            }
        }
        else
        {
            Log.Warning($"{EntityManager.GetComponent<MetaDataComponent>(entity).EntityName} has a {nameof(LightBehaviourComponent)} but it has no {nameof(PointLightComponent)}! Check the prototype!");
        }
    }

    /// <summary>
    /// Start animating a light behaviour with the specified ID. If the specified ID is empty, it will start animating all light behaviour entries.
    /// If specified light behaviours are already animating, calling this does nothing.
    /// Multiple light behaviours can have the same ID.
    /// </summary>
    public void StartLightBehaviour(Entity<LightBehaviourComponent> entity, string id = "")
    {
        if (!EntityManager.TryGetComponent(entity, out AnimationPlayerComponent? animation))
        {
            return;
        }

        foreach (var container in entity.Comp.Animations)
        {
            if (container.LightBehaviour.ID == id || id == string.Empty)
            {
                if (!_player.HasRunningAnimation(entity, animation, LightBehaviourComponent.KeyPrefix + container.Key))
                {
                    CopyLightSettings(entity, container.LightBehaviour.Property);
                    container.LightBehaviour.UpdatePlaybackValues(container.Animation);
                    _player.Play(entity, container.Animation, LightBehaviourComponent.KeyPrefix + container.Key);
                }
            }
        }
    }

    /// <summary>
    /// If any light behaviour with the specified ID is animating, then stop it.
    /// If no ID is specified then all light behaviours will be stopped.
    /// Multiple light behaviours can have the same ID.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="removeBehaviour">Should the behaviour(s) also be removed permanently?</param>
    /// <param name="resetToOriginalSettings">Should the light have its original settings applied?</param>
    public void StopLightBehaviour(Entity<LightBehaviourComponent> entity, string id = "", bool removeBehaviour = false, bool resetToOriginalSettings = false)
    {
        if (!EntityManager.TryGetComponent(entity, out AnimationPlayerComponent? animation))
        {
            return;
        }

        var comp = entity.Comp;

        var toRemove = new List<LightBehaviourComponent.AnimationContainer>();

        foreach (var container in comp.Animations)
        {
            if (container.LightBehaviour.ID == id || id == string.Empty)
            {
                if (_player.HasRunningAnimation(entity, animation, LightBehaviourComponent.KeyPrefix + container.Key))
                {
                    _player.Stop(entity, animation, LightBehaviourComponent.KeyPrefix + container.Key);
                }

                if (removeBehaviour)
                {
                    toRemove.Add(container);
                }
            }
        }

        foreach (var container in toRemove)
        {
            comp.Animations.Remove(container);
        }

        if (resetToOriginalSettings && EntityManager.TryGetComponent(entity, out PointLightComponent? light))
        {
            foreach (var (property, value) in comp.OriginalPropertyValues)
            {
                AnimationHelper.SetAnimatableProperty(light, property, value);
            }
        }

        comp.OriginalPropertyValues.Clear();
    }

    /// <summary>
    /// Checks if at least one behaviour is running.
    /// </summary>
    /// <returns>Whether at least one behaviour is running, false if none is.</returns>
    public bool HasRunningBehaviours(Entity<LightBehaviourComponent> entity)
    {
        //var uid = Owner;
        if (!EntityManager.TryGetComponent(entity, out AnimationPlayerComponent? animation))
        {
            return false;
        }

        return entity.Comp.Animations.Any(container => _player.HasRunningAnimation(entity, animation, LightBehaviourComponent.KeyPrefix + container.Key));
    }

    /// <summary>
    /// Add a new light behaviour to the component and start it immediately unless otherwise specified.
    /// </summary>
    public void AddNewLightBehaviour(Entity<LightBehaviourComponent> entity, LightBehaviourAnimationTrack behaviour, bool playImmediately = true)
    {
        var key = 0;
        var comp = entity.Comp;

        while (comp.Animations.Any(x => x.Key == key))
        {
            key++;
        }

        var animation = new Animation()
        {
            AnimationTracks = { behaviour }
        };

        behaviour.Initialize(entity.Owner, _random, EntityManager);

        var container = new LightBehaviourComponent.AnimationContainer(key, animation, behaviour);
        comp.Animations.Add(container);

        if (playImmediately)
        {
            StartLightBehaviour(entity, behaviour.ID);
        }
    }
}
