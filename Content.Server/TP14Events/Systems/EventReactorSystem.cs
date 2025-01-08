using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Server.Event.Components;
using Robust.Shared.Random;
using Content.Server.Chat.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Event.Systems;

/// <summary>
/// This handles reactor events like light flickering and global announcements for the floatsam event.
/// </summary>
public sealed class EventReactorSystem : EntitySystem
{

    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;

    private float _updateTimer = 0f;
    private float _flickerTimer = 0f;
    private const float UpdateInterval = 1f;
    private const float FlickerInterval = 100f;
    
    /// <inheritdoc/>
    public override void Initialize()
    {
    }

     public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _updateTimer += frameTime;

            _flickerTimer += frameTime;

            if (_updateTimer >= UpdateInterval)
            {
                foreach (var entity in EntityManager.EntityQuery<EventReactorComponent>())
                {
                    
                    EntityUid uid = entity.Owner;
                    var component = EntityManager.GetComponent<EventReactorComponent>(uid);
                    var timer = EntityManager.GetComponent<ActiveTimerTriggerComponent>(uid);
                    ReactorCheck(uid, component, timer);
                }
                _updateTimer = 0f;
            }

            if (_flickerTimer >= FlickerInterval)
            {
                foreach (var entity in EntityManager.EntityQuery<EventReactorComponent>())
                {
                    EntityUid uid = entity.Owner;
                    FlickerReactor(uid);
                }
                _flickerTimer = 0f;
            }
        }


     private void ReactorCheck(EntityUid uid, EventReactorComponent component, ActiveTimerTriggerComponent timer)
     {
        var lights = GetEntityQuery<PoweredLightComponent>();
        foreach (var light in _lookup.GetEntitiesInRange(uid, component.Radius, LookupFlags.StaticSundries ))
        {
            if (!lights.HasComponent(light))
                continue;

            if (!_random.Prob(component.FlickerChance))
                continue;
            if (timer.TimeRemaining <= 3600 && !component.FirstWarning)
            {
                 _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("first-alert-warning"), component.title, announcementSound: component.Sound, colorOverride: component.Color);
                component.FirstWarning = true;
            }

            if (timer.TimeRemaining <= 2600 && !component.SecondWarning)
            {
                 _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("second-alert-warning"), component.title, announcementSound: component.Sound, colorOverride: component.Color);
                component.SecondWarning = true;
            }

            if (timer.TimeRemaining <= 1800 && !component.ThirdWarning)
            {
                 _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("third-alert-warning"), component.title, announcementSound: component.Sound, colorOverride: component.Color);
                component.ThirdWarning = true;
            }

            if (timer.TimeRemaining <= 30 && !component.MeltdownWarning)
            {
                 _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("meltdown-alert-warning"), component.title, announcementSound: component.Sound, colorOverride: component.Color);
                component.MeltdownWarning = true;
            }
        }
    }

     private void FlickerReactor(EntityUid uid, EventReactorComponent component)
     {
        var lights = GetEntityQuery<PoweredLightComponent>();
        foreach (var light in _lookup.GetEntitiesInRange(uid, component.Radius, LookupFlags.StaticSundries ))
        {
            if (!lights.HasComponent(light))
                continue;

            if (!_random.Prob(component.FlickerChance))
                continue;

            _ghost.DoGhostBooEvent(light);
        }
    }
}
