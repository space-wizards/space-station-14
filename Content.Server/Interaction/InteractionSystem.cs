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

            var ev = new CanInteractWhileInsideContainerEvent(user, container);
            RaiseLocalEvent(target, ev);
            if (!ev.Handled)
                return false;

            return true;
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

    public sealed class CanInteractWhileInsideContainerEvent : HandledEntityEventArgs
    {
        public readonly EntityUid User;
        public readonly BaseContainer Container;

        public CanInteractWhileInsideContainerEvent(EntityUid user, BaseContainer container)
        {
            User = user;
            Container = container;
        }
    }
}
