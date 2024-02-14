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
        SubscribeLocalEvent<ClusterGrenadeComponent, TriggerEvent>(OnClugTrigger);
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
            component.UnspawnedCount = Math.Max(0, component.MaxGrenades - component.GrenadesContainer.ContainedEntities.Count);
            UpdateAppearance(clug);
        }
    }

    private void OnClugUsing(Entity<ClusterGrenadeComponent> clug, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var component = clug.Comp;

        // TODO: Should use whitelist.
        if (component.GrenadesContainer.ContainedEntities.Count >= component.MaxGrenades ||
            !HasComp<FlashOnTriggerComponent>(args.Used))
            return;

        _containerSystem.Insert(args.Used, component.GrenadesContainer);
        UpdateAppearance(clug);
        args.Handled = true;
    }

    private void OnClugTrigger(Entity<ClusterGrenadeComponent> clug, ref TriggerEvent args)
    {
        var component = clug.Comp;
        component.CountDown = true;
        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<ClusterGrenadeComponent>();

        while (query.MoveNext(out var uid, out var clug))
        {
            if (clug.CountDown && clug.UnspawnedCount > 0)
            {
                var grenadesInserted = clug.GrenadesContainer.ContainedEntities.Count + clug.UnspawnedCount;
                var thrownCount = 0;
                var segmentAngle = 360 / grenadesInserted;
                var grenadeDelay = 0f;

                while (TryGetGrenade(uid, clug, out var grenade))
                {
                    // var distance = random.NextFloat() * _throwDistance;
                    var angleMin = segmentAngle * thrownCount;
                    var angleMax = segmentAngle * (thrownCount + 1);
                    var angle = Angle.FromDegrees(_random.Next(angleMin, angleMax));
                    if (clug.RandomAngle)
                        angle = _random.NextAngle();
                    thrownCount++;

                    switch (clug.GrenadeType)
                    {
                        case GrenadeType.Shoot:
                            ShootProjectile(grenade, angle, clug, uid);
                            break;
                        case GrenadeType.Throw:
                            ThrowGrenade(grenade, angle, clug);
                            break;
                    }

                    // give an active timer trigger to the contained grenades when they get launched
                    if (clug.TriggerGrenades)
                    {
                        grenadeDelay += _random.NextFloat(clug.GrenadeTriggerIntervalMin, clug.GrenadeTriggerIntervalMax);
                        var grenadeTimer = EnsureComp<ActiveTimerTriggerComponent>(grenade);
                        grenadeTimer.TimeRemaining = (clug.BaseTriggerDelay + grenadeDelay);
                        var ev = new ActiveTimerTriggerEvent(grenade, uid);
                        RaiseLocalEvent(uid, ref ev);
                    }
                }
                // delete the empty shell of the clusterbomb
                Del(uid);
            }
        }
    }

    private void ShootProjectile(EntityUid grenade, Angle angle, ClusterGrenadeComponent clug, EntityUid clugUid)
    {
        var direction = angle.ToVec().Normalized();

        if (clug.RandomSpread)
            direction = _random.NextVector2().Normalized();

        _gun.ShootProjectile(grenade, direction, Vector2.One.Normalized(), clugUid);

    }

    private void ThrowGrenade(EntityUid grenade, Angle angle, ClusterGrenadeComponent clug)
    {
        var direction = angle.ToVec().Normalized() * clug.Distance;

        if (clug.RandomSpread)
            direction = angle.ToVec().Normalized() * _random.NextFloat(clug.MinSpreadDistance, clug.MaxSpreadDistance);

        _throwingSystem.TryThrow(grenade, direction, clug.Velocity);
    }

    private bool TryGetGrenade(EntityUid clugUid, ClusterGrenadeComponent component, out EntityUid grenade)
    {
        grenade = default;

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
