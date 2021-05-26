using System;
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.EntitySystems
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

            // The following subscriptions are basically the "method calls" of this entity system.
            SubscribeLocalEvent<StackComponent, StackUseEvent>(OnStackUse);
            SubscribeLocalEvent<StackComponent, StackSplitEvent>(OnStackSplit);
            SubscribeLocalEvent<StackTypeSpawnEvent>(OnStackTypeSpawn);
        }

        /// <summary>
        ///     Try to use an amount of items on this stack.
        ///     See <see cref="StackUseEvent"/>
        /// </summary>
        private void OnStackUse(EntityUid uid, StackComponent stack, StackUseEvent args)
        {
            // Check if we have enough things in the stack for this...
            if (stack.Count < args.Amount)
            {
                // Not enough things in the stack, so we set the output result to false.
                args.Result = false;
            }
            else
            {
                // We do have enough things in the stack, so remove them and set the output result to true.
                RaiseLocalEvent(uid, new StackChangeCountEvent(stack.Count - args.Amount));
                args.Result = true;
            }
        }

        /// <summary>
        ///     Try to split this stack into two.
        ///     See <see cref="StackSplitEvent"/>
        /// </summary>
        private void OnStackSplit(EntityUid uid, StackComponent stack, StackSplitEvent args)
        {
            // If the stack doesn't have enough things as specified in the parameters, we do nothing.
            if (stack.Count < args.Amount)
                return;

            // Get a prototype ID to spawn the new entity. Null is also valid, although it should rarely be picked...
            var prototype = _prototypeManager.TryIndex<StackPrototype>(stack.StackTypeId, out var stackType)
                ? stackType.Spawn
                : stack.Owner.Prototype?.ID ?? null;

            // Remove the amount of things we want to split from the original stack...
            RaiseLocalEvent(uid, new StackChangeCountEvent(stack.Count - args.Amount));

            // Set the output parameter in the event instance to the newly split stack.
            args.Result = EntityManager.SpawnEntity(prototype, args.SpawnPosition);

            if (args.Result.TryGetComponent(out StackComponent? stackComp))
            {
                // Set the split stack's count.
                RaiseLocalEvent(args.Result.Uid, new StackChangeCountEvent(args.Amount));
            }
        }

        /// <summary>
        ///     Tries to spawn a stack of a certain type.
        ///     See <see cref="StackTypeSpawnEvent"/>
        /// </summary>
        private void OnStackTypeSpawn(StackTypeSpawnEvent args)
        {
            // Can't spawn a stack for an invalid type.
            if (args.StackType == null)
                return;

            // Set the output result parameter to the new stack entity...
            args.Result = EntityManager.SpawnEntity(args.StackType.Spawn, args.SpawnPosition);
            var stack = args.Result.GetComponent<StackComponent>();

            // And finally, set the correct amount!
            RaiseLocalEvent(args.Result.Uid, new StackChangeCountEvent(args.Amount));
        }

        private void OnStackInteractUsing(EntityUid uid, StackComponent stack, InteractUsingEvent args)
        {
            if (!args.Used.TryGetComponent<StackComponent>(out var otherStack))
                return;

            if (!otherStack.StackTypeId.Equals(stack.StackTypeId))
                return;

            var toTransfer = Math.Min(stack.Count, otherStack.AvailableSpace);
            RaiseLocalEvent(uid, new StackChangeCountEvent(stack.Count - toTransfer));
            RaiseLocalEvent(args.Used.Uid, new StackChangeCountEvent(otherStack.Count + toTransfer));

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

    /*
     * The following events are actually funny ECS method calls!
     *
     * Instead of coupling systems together into a ball of spaghetti,
     * we raise events that act as method calls.
     *
     * So for example, instead of having an Use() method in the
     * stack component or stack system, we have a StackUseEvent.
     * Before raising the event, you would set the Amount property,
     * which acts as a parameter or argument, and afterwards the
     * entity system in charge of handling this would perform the logic
     * and then set the Result on the event instance.
     * Then you can access this property to see whether your Use attempt succeeded.
     *
     * This is very powerful, as it completely removes the coupling
     * between entity systems and allows for greater flexibility.
     * If you want to intercept this event with another entity system, you can.
     * And you don't have to write any bad, hacky code for this!
     * You could even use handled events, or cancellable events...
     * The possibilities are endless.
     *
     * Of course, not everything needs to be directed events!
     * Broadcast events also work in the same way.
     * For example, we use a broadcast event to spawn a stack of a certain type.
     *
     * Wrapping your head around this may be difficult at first,
     * but soon you'll get it, coder. Soon you'll grasp the wisdom.
     * Go forth and write some beautiful and robust code!
     */

    /// <summary>
    ///     Uses an amount of things from a stack.
    ///     Whether this succeeded is stored in <see cref="Result"/>.
    /// </summary>
    public class StackUseEvent : EntityEventArgs
    {
        /// <summary>
        ///     The amount of things to use on the stack.
        ///     Consider this the equivalent of a parameter for a method call.
        /// </summary>
        public int Amount { get; init; }

        /// <summary>
        ///     Whether the action succeeded or not.
        ///     Set by the <see cref="StackSystem"/> after handling this event.
        ///     Consider this the equivalent of a return value for a method call.
        /// </summary>
        public bool Result { get; set; } = false;
    }

    /// <summary>
    ///     Tries to split a stack into two.
    ///     If this succeeds, <see cref="Result"/> will be the new stack.
    /// </summary>
    public class StackSplitEvent : EntityEventArgs
    {
        /// <summary>
        ///     The amount of things to take from the original stack.
        ///     Input parameter.
        /// </summary>
        public int Amount { get; init; }

        /// <summary>
        ///     The position where to spawn the new stack.
        ///     Input parameter.
        /// </summary>
        public EntityCoordinates SpawnPosition { get; init; }

        /// <summary>
        ///     The newly split stack. May be null if the split failed.
        ///     Output parameter.
        /// </summary>
        public IEntity? Result { get; set; } = null;
    }

    /// <summary>
    ///     Tries to spawn a stack of a certain type.
    ///     If this succeeds, <see cref="Result"/> will be the new stack.
    /// </summary>
    public class StackTypeSpawnEvent : EntityEventArgs
    {
        /// <summary>
        ///     The amount of things the spawned stack will have.
        ///     Input parameter.
        /// </summary>
        public int Amount { get; init; }

        /// <summary>
        ///     The stack type to be spawned.
        ///     Input parameter.
        /// </summary>
        public StackPrototype? StackType { get; init; }

        /// <summary>
        ///     The position where the new stack will be spawned.
        ///     Input parameter.
        /// </summary>
        public EntityCoordinates SpawnPosition { get; init; }

        /// <summary>
        ///     The newly spawned stack, or null if this failed.
        ///     Output parameter.
        /// </summary>
        public IEntity? Result { get; set; } = null;
    }
}
