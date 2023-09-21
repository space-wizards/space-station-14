

using Content.Server.Administration.Logs;
using Content.Server.Pulling;
using Content.Shared.ActionBlocker;
using Content.Shared.DragDrop;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Pulling.Components;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Random;

namespace Content.Server.Interaction
{
    /// <summary>
    /// Governs interactions during clicking on entities
    /// </summary>
    [UsedImplicitly]
    public sealed partial class InteractionSystem : SharedInteractionSystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<DragDropRequestEvent>(HandleDragDropRequestEvent);

            SubscribeLocalEvent<BoundUserInterfaceCheckRangeEvent>(HandleUserInterfaceRangeCheck);
        }

        public override bool CanAccessViaStorage(EntityUid user, EntityUid target)
        {
            if (Deleted(target))
                return false;

            if (!_container.TryGetContainingContainer(target, out var container))
                return false;

            if (!TryComp(container.Owner, out StorageComponent? storage))
                return false;

            if (storage.Container?.ID != container.ID)
                return false;

            if (!TryComp(user, out ActorComponent? actor))
                return false;

            // we don't check if the user can access the storage entity itself. This should be handed by the UI system.
            return _uiSystem.SessionHasOpenUi(container.Owner, StorageComponent.StorageUiKey.Key, actor.PlayerSession);
        }

        #region Drag drop

        private void HandleDragDropRequestEvent(DragDropRequestEvent msg, EntitySessionEventArgs args)
        {
            var dragged = GetEntity(msg.Dragged);
            var target = GetEntity(msg.Target);

            if (Deleted(dragged) || Deleted(target))
                return;

            var user = args.SenderSession.AttachedEntity;

            if (user == null || !_actionBlockerSystem.CanInteract(user.Value, target))
                return;

            // must be in range of both the target and the object they are drag / dropping
            // Client also does this check but ya know we gotta validate it.
            if (!InRangeUnobstructed(user.Value, dragged, popup: true)
                || !InRangeUnobstructed(user.Value, target, popup: true))
            {
                return;
            }

            var dragArgs = new DragDropDraggedEvent(user.Value, target);

            // trigger dragdrops on the dropped entity
            RaiseLocalEvent(dragged, ref dragArgs);

            if (dragArgs.Handled)
                return;

            var dropArgs = new DragDropTargetEvent(user.Value, dragged);

            // trigger dragdrops on the target entity (what you are dropping onto)
            RaiseLocalEvent(GetEntity(msg.Target), ref dropArgs);
        }

        #endregion

        private void HandleUserInterfaceRangeCheck(ref BoundUserInterfaceCheckRangeEvent ev)
        {
            if (ev.Player.AttachedEntity is not { } user)
                return;

            if (InRangeUnobstructed(user, ev.Target, ev.UserInterface.InteractionRange))
            {
                ev.Result = BoundUserInterfaceRangeResult.Pass;
            }
            else
            {
                ev.Result = BoundUserInterfaceRangeResult.Fail;
            }
        }
    }
}
