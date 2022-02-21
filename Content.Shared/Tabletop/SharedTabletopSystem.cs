using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Stunnable;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop
{
    public abstract class SharedTabletopSystem : EntitySystem
    {
        [Dependency] protected readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        [Serializable, NetSerializable]
        public sealed class TabletopDraggableComponentState : ComponentState
        {
            public NetUserId? DraggingPlayer;

            public TabletopDraggableComponentState(NetUserId? draggingPlayer)
            {
                DraggingPlayer = draggingPlayer;
            }
        }

        #region Utility

        /// <summary>
        /// Whether the table exists, and the player can interact with it.
        /// </summary>
        /// <param name="playerEntity">The player entity to check.</param>
        /// <param name="table">The table entity to check.</param>
        protected bool CanSeeTable(EntityUid playerEntity, EntityUid? table)
        {
            if (table == null)
                return false;

            if (EntityManager.GetComponent<TransformComponent>(table.Value).Parent?.Owner is not { } parent)
            {
                return false;
            }

            if (!EntityManager.HasComponent<MapComponent>(parent) && !EntityManager.HasComponent<IMapGridComponent>(parent))
            {
                return false;
            }

            return _interactionSystem.InRangeUnobstructed(playerEntity, table.Value) && _actionBlockerSystem.CanInteract(playerEntity, table);
        }

        protected bool StunnedOrNoHands(EntityUid playerEntity)
        {
            var stunned = EntityManager.HasComponent<StunnedComponent>(playerEntity);
            var hasHand = EntityManager.TryGetComponent<SharedHandsComponent>(playerEntity, out var handsComponent) &&
                          handsComponent.Hands.Count > 0;

            return stunned || !hasHand;
        }

        #endregion
    }
}
