

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
