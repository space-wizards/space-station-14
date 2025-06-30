using Content.Server.Hands.Systems;
using Content.Shared.Construction;
using Content.Shared.Hands.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class EmptyAllContainers : IGraphAction
    {
        /// <summary>
        ///     Whether or not the user should attempt to pick up the removed entities.
        /// </summary>
        [DataField]
        public bool Pickup = false;

        /// <summary>
        ///    Whether or not to empty the container at the user's location.
        /// </summary>
        [DataField]
        public bool EmptyAtUser = false;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (!entityManager.TryGetComponent(uid, out ContainerManagerComponent? containerManager))
                return;

            var containerSys = entityManager.EntitySysManager.GetEntitySystem<SharedContainerSystem>();
            var handSys = entityManager.EntitySysManager.GetEntitySystem<HandsSystem>();
            var transformSys = entityManager.EntitySysManager.GetEntitySystem<TransformSystem>();

            HandsComponent? hands = null;
            var pickup = Pickup && entityManager.TryGetComponent(userUid, out hands);

            foreach (var container in containerSys.GetAllContainers(uid))
            {
                foreach (var ent in containerSys.EmptyContainer(container, true, reparent: !pickup))
                {
                    if (EmptyAtUser && userUid is not null)
                        transformSys.DropNextTo(ent, (EntityUid) userUid);

                    if (pickup)
                        handSys.PickupOrDrop(userUid, ent, handsComp: hands);
                }
            }
        }
    }
}
