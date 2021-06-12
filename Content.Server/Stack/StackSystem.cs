using System;
using Content.Shared.Interaction;
using Content.Shared.Notification;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Stack
{
    /// <summary>
    ///     Entity system that handles everything relating to stacks.
    ///     This is a good example for learning how to code in an ECS manner.
    /// </summary>
    [UsedImplicitly]
    public class StackSystem : SharedStackSystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StackComponent, InteractUsingEvent>(OnStackInteractUsing);
        }

        /// <summary>
        ///     Try to use an amount of items on this stack. Returns whether this succeeded.
        /// </summary>
        public bool Use(EntityUid uid, SharedStackComponent stack, int amount)
        {
            // Check if we have enough things in the stack for this...
            if (stack.Count < amount)
            {
                // Not enough things in the stack, return false.
                return false;
            }

            // We do have enough things in the stack, so remove them and change.
            SetCount(uid, stack, stack.Count - amount);
            return true;
        }

        /// <summary>
        ///     Try to split this stack into two. Returns a non-null <see cref="IEntity"/> if successful.
        /// </summary>
        public IEntity? Split(EntityUid uid, SharedStackComponent stack, int amount, EntityCoordinates spawnPosition)
        {
            // Get a prototype ID to spawn the new entity. Null is also valid, although it should rarely be picked...
            var prototype = _prototypeManager.TryIndex<StackPrototype>(stack.StackTypeId, out var stackType)
                ? stackType.Spawn
                : stack.Owner.Prototype?.ID ?? null;

            // Try to remove the amount of things we want to split from the original stack...
            if (!Use(uid, stack, amount))
                return null;

            // Set the output parameter in the event instance to the newly split stack.
            var entity = EntityManager.SpawnEntity(prototype, spawnPosition);

            if (ComponentManager.TryGetComponent(entity.Uid, out SharedStackComponent? stackComp))
            {
                // Set the split stack's count.
                SetCount(entity.Uid, stackComp, amount);
            }

            return entity;
        }

        /// <summary>
        ///     Spawns a stack of a certain stack type. See <see cref="StackPrototype"/>.
        /// </summary>
        public IEntity Spawn(int amount, StackPrototype prototype, EntityCoordinates spawnPosition)
        {
            // Set the output result parameter to the new stack entity...
            var entity = EntityManager.SpawnEntity(prototype.Spawn, spawnPosition);
            var stack = ComponentManager.GetComponent<StackComponent>(entity.Uid);

            // And finally, set the correct amount!
            SetCount(entity.Uid, stack, amount);
            return entity;
        }

        private void OnStackInteractUsing(EntityUid uid, StackComponent stack, InteractUsingEvent args)
        {
            if (!args.Used.TryGetComponent<StackComponent>(out var otherStack))
                return;

            if (!otherStack.StackTypeId.Equals(stack.StackTypeId))
                return;

            var toTransfer = Math.Min(stack.Count, otherStack.AvailableSpace);
            SetCount(uid, stack, stack.Count - toTransfer);
            SetCount(args.Used.Uid, otherStack, otherStack.Count + toTransfer);

            var popupPos = args.ClickLocation;
            if (!popupPos.IsValid(EntityManager))
            {
                popupPos = args.User.Transform.Coordinates;
            }

            switch (toTransfer)
            {
                case > 0:
                    popupPos.PopupMessage(args.User, $"+{toTransfer}");

                    if (otherStack.AvailableSpace == 0)
                    {
                        args.Used.SpawnTimer(
                            300,
                            () => popupPos.PopupMessage(
                                args.User,
                                Loc.GetString("comp-stack-becomes-full")
                            )
                        );
                    }

                    break;

                case 0 when otherStack.AvailableSpace == 0:
                    popupPos.PopupMessage(
                        args.User,
                        Loc.GetString("comp-stack-already-full")
                    );
                    break;
            }

            args.Handled = true;
        }
    }
}
