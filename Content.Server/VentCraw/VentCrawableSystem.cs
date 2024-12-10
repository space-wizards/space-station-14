// Initial file ported from the Starlight project repo, located at https://github.com/ss14Starlight/space-station-14

using System.Linq;
using Content.Shared.VentCraw.Tube.Components;
using Content.Shared.VentCraw.Components;
using Content.Shared.VentCraw;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Containers;
using Content.Shared.Eye.Blinding.Systems;

namespace Content.Server.VentCraw;

public sealed class VentCrawableSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly BlindableSystem _blind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VentCrawHolderComponent, VentCrawExitEvent>(OnVentCrawExitEvent);
    }

    /// <summary>
    /// Exits the vent craws for the specified VentCrawHolderComponent, removing it and any contained entities from the craws.
    /// </summary>
    /// <param name="uid">The EntityUid of the VentCrawHolderComponent.</param>
    /// <param name="holder">The VentCrawHolderComponent instance.</param>
    /// <param name="holderTransform">The TransformComponent instance for the VentCrawHolderComponent.</param>
    private void OnVentCrawExitEvent(EntityUid uid, VentCrawHolderComponent holder, ref VentCrawExitEvent args)
    {
        var holderTransform = args.holderTransform;

        if (Terminating(uid))
            return;

        if (!Resolve(uid, ref holderTransform))
            return;

        if (holder.IsExitingVentCraws)
        {
            Log.Error("Tried exiting VentCraws twice. This should never happen.");
            return;
        }

        holder.IsExitingVentCraws = true;

        foreach (var entity in holder.Container.ContainedEntities.ToArray())
        {
            RemComp<BeingVentCrawComponent>(entity);

            var meta = MetaData(entity);
            _containerSystem.Remove(entity, holder.Container, reparent: false, force: true);

            var xform = Transform(entity);
            if (xform.ParentUid != uid)
                continue;

            _xformSystem.AttachToGridOrMap(entity, xform);

            if (TryComp<VentCrawlerComponent>(entity, out var ventCrawComp))
            {
                ventCrawComp.InTube = false;
                Dirty(entity , ventCrawComp);
            }

            if (EntityManager.TryGetComponent(entity, out PhysicsComponent? physics))
            {
                _physicsSystem.WakeBody(entity, body: physics);
            }

            _blind.UpdateIsBlind(entity);
        }

        EntityManager.DeleteEntity(uid);
    }
}
