using System;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Helpers;
using Content.Shared.MobState.Components;
using Content.Shared.Stunnable;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop
{
    public abstract class SharedTabletopSystem : EntitySystem
    {
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
        /// Whether the table exists, is in range and the player is alive.
        /// </summary>
        /// <param name="playerEntity">The player entity to check.</param>
        /// <param name="table">The table entity to check.</param>
        protected static bool CanSeeTable(IEntity playerEntity, IEntity? table)
        {
            if (table?.Transform.Parent?.Owner is not { } parent)
            {
                return false;
            }

            if (!parent.HasComponent<MapComponent>() && !parent.HasComponent<IMapGridComponent>())
            {
                return false;
            }

            var alive = playerEntity.TryGetComponent<MobStateComponent>(out var mob) && mob.IsAlive();
            var inRange = playerEntity.InRangeUnobstructed(table);

            return alive && inRange;
        }

        protected static bool StunnedOrNoHands(IEntity playerEntity)
        {
            var stunned = playerEntity.TryGetComponent<SharedStunnableComponent>(out var stun) &&
                          stun.Stunned;
            var hasHand = playerEntity.TryGetComponent<SharedHandsComponent>(out var handsComponent) &&
                          handsComponent.Hands.Count > 0;

            return stunned || !hasHand;
        }

        #endregion
    }
}
