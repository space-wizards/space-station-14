using Content.Server.Event.Components;
using Robust.Shared.Prototypes;
using Content.Server.Deathwhale;
using Content.Server.Falling;

namespace Content.Shared.Deathwhale;

public sealed class DeathWhaleSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private const float UpdateInterval = 1f;
    private float _updateTimer = 0f;

    private const float KillInterval = 3f;
    private float _killTimer = 0f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeathWhaleComponent, ComponentInit>(OnCompInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _updateTimer += frameTime;
        _killTimer += frameTime;

        if (_updateTimer >= UpdateInterval)
        {
            foreach (var entity in EntityManager.EntityQuery<DeathWhaleComponent>())
            {
                var uid = entity.Owner;
                var component = EntityManager.GetComponent<DeathWhaleComponent>(uid); // Get the DeathWhaleComponent

                DeathWhaleCheck(uid, component);
            }

            // Reset the timer
            _updateTimer = 0f;
        }

         if (_killTimer >= KillInterval)
        {
             foreach (var entity in EntityManager.EntityQuery<DeathWhaleComponent>())
            {
                var uid = entity.Owner;
                var component = EntityManager.GetComponent<DeathWhaleComponent>(uid);
                QueueDel(component.caughtPrey);
                component.caughtPrey = null;
            }
           
        }

    }

    // Log message when the component is initialized
    private void OnCompInit(EntityUid uid, DeathWhaleComponent component, ComponentInit args)
    {
        Log.Info($"Deathwhale initialized for entity {uid}");
    }

    private void DeathWhaleCheck(EntityUid uid, DeathWhaleComponent component)
    {
        // Iterate through all entities within the DeathWhale's radius and check if they have a FallSystemComponent
        foreach (var prey in _lookup.GetEntitiesInRange(uid, component.Radius, LookupFlags.StaticSundries))
        {
            if (!EntityManager.HasComponent<FallSystemComponent>(prey))
            {
                continue;
            }
            if (component.caughtPrey == null)
            {
                var preycaught = EnsureComp<FultonedComponent>(prey); // This will harpoon the prey and drag them up offscreen to be eaten
                preycaught.Removeable = false;
                preycaught.Beacon = uid;
                component.caughtPrey = prey;
                _killTimer = 0f;
            }
        }
    }
}
