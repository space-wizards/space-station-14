using Content.Server.Explosion.Components;
using Content.Server.Flash.Components;
using Content.Shared.Explosion;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Random;

namespace Content.Server.Explosion.EntitySystems;

public sealed class ClusterGrenadeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClusterGrenadeComponent, ComponentInit>(OnClugInit);
        SubscribeLocalEvent<ClusterGrenadeComponent, ComponentStartup>(OnClugStartup);
        SubscribeLocalEvent<ClusterGrenadeComponent, InteractUsingEvent>(OnClugUsing);
        SubscribeLocalEvent<ClusterGrenadeComponent, UseInHandEvent>(OnClugUse);
    }

    private void OnClugInit(EntityUid uid, ClusterGrenadeComponent component, ComponentInit args)
    {
        component.GrenadesContainer = _container.EnsureContainer<Container>(uid, "cluster-flash");
    }

    private void OnClugStartup(EntityUid uid, ClusterGrenadeComponent component, ComponentStartup args)
    {
        if (component.FillPrototype != null)
        {
            component.UnspawnedCount = Math.Max(0, component.MaxGrenades - component.GrenadesContainer.ContainedEntities.Count);
            UpdateAppearance(component);
        }
    }

    private void OnClugUsing(EntityUid uid, ClusterGrenadeComponent component, InteractUsingEvent args)
    {
        if (args.Handled) return;

        // TODO: Should use whitelist.
        if (component.GrenadesContainer.ContainedEntities.Count >= component.MaxGrenades ||
            !HasComp<FlashOnTriggerComponent>(args.Used))
            return;

        component.GrenadesContainer.Insert(args.Used);
        UpdateAppearance(component);
        args.Handled = true;
    }

    private void OnClugUse(EntityUid uid, ClusterGrenadeComponent component, UseInHandEvent args)
    {
        if (component.CountDown || (component.GrenadesContainer.ContainedEntities.Count + component.UnspawnedCount) <= 0)
            return;

        // TODO: Should be an Update loop
        if (Deleted(component.Owner))
            return;

        component.CountDown = true;
        var delay = 20;
        var grenadesInserted = component.GrenadesContainer.ContainedEntities.Count + component.UnspawnedCount;
        var segmentAngle = 360 / grenadesInserted;
        // Welcome to nesting hell.
        var throwDelay = 60 / component.ThrowsPerSecond;
        uid.SpawnTimer(delay, () =>
        {
            for (var thrownCount = 0; thrownCount < grenadesInserted; thrownCount++)
            {
                uid.SpawnTimer(throwDelay * thrownCount, () =>
                {
                    TryGetGrenade(component, out var grenade);
                    var angleMin = segmentAngle * thrownCount;
                    var angleMax = segmentAngle * (thrownCount + 1);
                    var angle = Angle.FromDegrees(_random.Next(angleMin, angleMax));
                    // var distance = random.NextFloat() * _throwDistance;

                    var physComp = EntityManager.GetComponent<PhysicsComponent>(grenade);
                    if (physComp.BodyType == BodyType.KinematicController)
                    {
                        physComp.BodyType = BodyType.Dynamic;
                        // TODO: Suss out throw strength
                        physComp.ApplyLinearImpulse(angle.ToVec().Normalized * component.ThrowDistance * 0.1f);
                        grenade.SpawnTimer(delay, () =>
                        {
                            physComp.BodyType = BodyType.KinematicController;
                        });
                    }
                    else
                    {
                        // TODO: Suss out throw strength
                        _throwingSystem.TryThrow(grenade, angle.ToVec().Normalized * component.ThrowDistance);
                    }

                    grenade.SpawnTimer(delay, () =>
                    {
                        if ((!EntityManager.EntityExists(grenade) ? EntityLifeStage.Deleted : MetaData(grenade).EntityLifeStage) >= EntityLifeStage.Deleted)
                            return;

                        _trigger.Trigger(grenade, args.User);
                    });
                });
            }
            uid.SpawnTimer(throwDelay * grenadesInserted, () =>
            {
                EntityManager.DeleteEntity(uid);
            });
        });

        args.Handled = true;
    }

    private bool TryGetGrenade(ClusterGrenadeComponent component, out EntityUid grenade)
    {
        grenade = default;

        if (component.UnspawnedCount > 0)
        {
            component.UnspawnedCount--;
            grenade = EntityManager.SpawnEntity(component.FillPrototype, Transform(component.Owner).MapPosition);
            return true;
        }

        if (component.GrenadesContainer.ContainedEntities.Count > 0)
        {
            grenade = component.GrenadesContainer.ContainedEntities[0];

            // This shouldn't happen but you never know.
            if (!component.GrenadesContainer.Remove(grenade))
                return false;

            return true;
        }

        return false;
    }

    private void UpdateAppearance(ClusterGrenadeComponent component)
    {
        if (!TryComp<AppearanceComponent>(component.Owner, out var appearance)) return;

        appearance.SetData(ClusterGrenadeVisuals.GrenadesCounter, component.GrenadesContainer.ContainedEntities.Count + component.UnspawnedCount);
    }
}
