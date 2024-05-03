using Content.Server.Explosion.Components;
using Content.Shared.Flash.Components;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Content.Server.Weapons.Ranged.Systems;
using System.Numerics;
using Content.Shared.Explosion.Components;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Content.Shared.Explosion.Components.OnTrigger;
using Content.Shared.Interaction.Events;

namespace Content.Server.Explosion.EntitySystems;

public sealed class ClusterGrenadeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClusterGrenadeComponent, ComponentInit>(OnClugInit);
        SubscribeLocalEvent<ClusterGrenadeComponent, ComponentStartup>(OnClugStartup);
        SubscribeLocalEvent<ClusterGrenadeComponent, InteractUsingEvent>(OnClugUsing);
        SubscribeLocalEvent<ClusterOnTriggerComponent, TriggerEvent>(HandleClusterTrigger);
    }

    private void OnClugInit(EntityUid uid, ClusterGrenadeComponent component, ComponentInit args)
    {
        component.GrenadesContainer = _container.EnsureContainer<Container>(uid, "cluster-payload");
    }

    private void OnClugStartup(Entity<ClusterGrenadeComponent> clug, ref ComponentStartup args)
    {
        var component = clug.Comp;
        if (component.FillPrototype != null)
        {
            component.UnspawnedCount = Math.Max(0, component.Capacity - component.GrenadesContainer.ContainedEntities.Count);
            UpdateAppearance(clug);
        }
    }

    private void OnClugUsing(Entity<ClusterGrenadeComponent> clug, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var component = clug.Comp;

        // TODO: Should use whitelist.
        if (component.GrenadesContainer.ContainedEntities.Count >= component.Capacity ||
            !HasComp<FlashOnTriggerComponent>(args.Used))
            return;

        _containerSystem.Insert(args.Used, component.GrenadesContainer);
        UpdateAppearance(clug);
        args.Handled = true;
    }

    private void HandleClusterTrigger(EntityUid uid, ClusterOnTriggerComponent component, ref TriggerEvent args)
    {
        SplitClusterGrenade(uid);
    }

    private void SplitClusterGrenade(EntityUid uid)
    {
        if (!TryComp<ClusterGrenadeComponent>(uid, out var clugComponent))
            return;

        var grenadesInserted = clugComponent.GrenadesContainer.ContainedEntities.Count + clugComponent.UnspawnedCount;
        var thrownCount = 0;
        var segmentAngle = 360 / grenadesInserted;
        var extraGrenadeDelay = 0f;

        while (TryGetGrenade(uid, clugComponent, out var grenade))
        {
            Logger.Debug($"got grenade {grenade}");
            var angleMin = segmentAngle * thrownCount;
            var angleMax = segmentAngle * (thrownCount + 1);
            var angle = Angle.FromDegrees(_random.Next(angleMin, angleMax));
            if (clugComponent.RandomAngle)
                angle = _random.NextAngle();
            thrownCount++;

            switch (clugComponent.GrenadeType)
            {
                case GrenadeType.Shoot:
                    // using grenade uid as the "gun" because using clug uid throws error due to it being deleted
                    ShootProjectile(grenade, angle, grenade, clugComponent); 
                    break;
                case GrenadeType.Throw:
                    // using grenade uid as the "thrower" because using clug uid throws error due to it being deleted
                    ThrowGrenade(grenade, angle, grenade, clugComponent); 
                    break;
            }

            // currently if I uncomment this section, it successfully goes through the code and whatnot
            // but after finishing up, the engine will proceed for a bit and then throw a
            // "Collection was modified; enumeration may not execute" error
            /*if (clugComponent.TriggerGrenades)
            {
                grenadeDelay += _random.NextFloat(clugComponent.GrenadeTriggerIntervalMin, clugComponent.GrenadeTriggerIntervalMax);
                var grenadeTimer = EnsureComp<ActiveTimerTriggerComponent>(grenade);
                grenadeTimer.TimeRemaining = (clugComponent.BaseTriggerDelay + grenadeDelay);
                var ev = new ActiveTimerTriggerEvent(grenade, uid);
                RaiseLocalEvent(uid, ref ev);
            }*/
        }
    }

    private void ShootProjectile(EntityUid grenade, Angle angle, EntityUid uid, ClusterGrenadeComponent clug)
    {
        Vector2 direction;
        var velocity = new Vector2(clug.Velocity, clug.Velocity);

        if (clug.RandomSpread)
            direction = _random.NextVector2().Normalized();
        else
            direction = angle.ToVec().Normalized();


        _gun.ShootProjectile(grenade, direction, velocity, uid);

    }

    private void ThrowGrenade(EntityUid grenade, Angle angle, EntityUid uid, ClusterGrenadeComponent clug)
    {
        Vector2 direction;

        if (clug.RandomSpread)
            direction = angle.ToVec().Normalized() * _random.NextFloat(clug.MinSpreadDistance, clug.MaxSpreadDistance);
        else
            direction = angle.ToVec().Normalized() * clug.Distance;

        _throwingSystem.TryThrow(grenade, direction, clug.Velocity, uid);
    }

    private bool TryGetGrenade(EntityUid clugUid, ClusterGrenadeComponent component, out EntityUid grenade)
    {
        if (component.UnspawnedCount > 0)
        {
            component.UnspawnedCount--;
            grenade = Spawn(component.FillPrototype, _transformSystem.GetMapCoordinates(clugUid));
            return true;
        }

        if (component.GrenadesContainer.ContainedEntities.Count > 0)
        {
            grenade = component.GrenadesContainer.ContainedEntities[0];

            // This shouldn't happen but you never know.
            if (!_containerSystem.Remove(grenade, component.GrenadesContainer))
                return false;

            return true;
        }

        grenade = default;
        return false;
    }

    private void UpdateAppearance(Entity<ClusterGrenadeComponent> clug)
    {
        var component = clug.Comp;
        if (!TryComp<AppearanceComponent>(clug, out var appearance))
            return;

        _appearance.SetData(clug, ClusterGrenadeVisuals.GrenadesCounter, component.GrenadesContainer.ContainedEntities.Count + component.UnspawnedCount, appearance);
    }
}
