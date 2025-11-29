using System.Linq;
using Content.Server.Hands.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Construction;
using Content.Shared.Hands.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
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

            // Use EntityStorageSystem for the entity_storage container to properly handle EnteringOffset.
            // This prevents entities from being placed inside walls when deconstructing wall closets.
            if (entityManager.TryGetComponent(uid, out EntityStorageComponent? storageComponent))
            {
                var entityStorageSys = entityManager.EntitySysManager.GetEntitySystem<EntityStorageSystem>();
                var contents = storageComponent.Contents.ContainedEntities.ToArray();
                entityStorageSys.EmptyContents(uid, storageComponent);

                // Handle Pickup and EmptyAtUser for emptied entities
                foreach (var ent in contents)
                {
                    if (EmptyAtUser && userUid is not null)
                        transformSys.DropNextTo(ent, (EntityUid) userUid);

                    if (pickup)
                        handSys.PickupOrDrop(userUid, ent, handsComp: hands);
                }
            }

            // Empty any other containers (e.g., paper_label slot on regular closets)
            foreach (var container in containerSys.GetAllContainers(uid))
            {
                // Skip the entity_storage container since it was already handled above
                if (container.ID == SharedEntityStorageSystem.ContainerName)
                    continue;

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
