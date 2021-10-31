using System;
using Content.Shared.ActionBlocker;
using Content.Shared.Ghost;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Helpers;
using Content.Shared.MobState.Components;
using Content.Shared.Stunnable;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop
{
    public abstract class SharedTabletopSystem : EntitySystem
    {
        [Dependency] protected readonly ActionBlockerSystem _actionBlockerSystem = default!;

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
        protected bool CanSeeTable(IEntity playerEntity, IEntity? table)
        {
            if (table?.Transform.Parent?.Owner is not { } parent)
            {
                return false;
            }

            if (!parent.HasComponent<MapComponent>() && !parent.HasComponent<IMapGridComponent>())
            {
                return false;
            }

            return _actionBlockerSystem.CanInteract(playerEntity.Uid);
        }

        protected static bool StunnedOrNoHands(IEntity playerEntity)
        {
            var stunned = playerEntity.HasComponent<StunnedComponent>();
            var hasHand = playerEntity.TryGetComponent<SharedHandsComponent>(out var handsComponent) &&
                          handsComponent.Hands.Count > 0;

            return stunned || !hasHand;
        }

        #endregion
    }
}
