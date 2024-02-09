using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;

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
            if (ev.Player.AttachedEntity is not { } user || ev.Result == BoundUserInterfaceRangeResult.Fail)
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
