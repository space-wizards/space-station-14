using Content.Server.Hands.Systems;
using Content.Shared.Construction;
using Content.Shared.Hands.Components;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class EmptyContainer : IGraphAction
    {
        [DataField("container")] public string Container { get; private set; } = string.Empty;

        /// <summary>
        ///     Whether or not the user should attempt to pick up the removed entities.
        /// </summary>
        [DataField("pickup")]
        public bool Pickup = false;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (!entityManager.TryGetComponent(uid, out ContainerManagerComponent? containerManager) ||
                !containerManager.TryGetContainer(Container, out var container)) return;

            var containerSys = entityManager.EntitySysManager.GetEntitySystem<ContainerSystem>();
            var handSys = entityManager.EntitySysManager.GetEntitySystem<HandsSystem>();

            HandsComponent? hands = null;
            var pickup = Pickup && entityManager.TryGetComponent(userUid, out hands);

            foreach (var ent in containerSys.EmptyContainer(container, true, reparent: !pickup))
            {
                if (pickup)
                    handSys.PickupOrDrop(userUid, ent, handsComp: hands);
            }
        }
    }
}
