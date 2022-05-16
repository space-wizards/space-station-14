using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Tabletop.Components;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;

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
            // Table may have been deleted, hence TryComp
            if (!TryComp(table, out MetaDataComponent? meta)
                || meta.EntityLifeStage >= EntityLifeStage.Terminating
                || (meta.Flags & MetaDataFlags.InContainer) == MetaDataFlags.InContainer)
            {
                return false;
            }

            return _interactionSystem.InRangeUnobstructed(playerEntity, table.Value) && _actionBlockerSystem.CanInteract(playerEntity, table);
        }

        protected bool CanDrag(EntityUid playerEntity, EntityUid target, [NotNullWhen(true)] out TabletopDraggableComponent? draggable)
        {
            if (!TryComp(target, out draggable))
                return false;

            // CanSeeTable checks interaction action blockers. So no need to check them here.
            // If this ever changes, so that ghosts can spectate games, then the check needs to be moved here.
            
            return TryComp(playerEntity, out SharedHandsComponent? hands) && hands.Hands.Count > 0;
        }
        #endregion
    }
}
