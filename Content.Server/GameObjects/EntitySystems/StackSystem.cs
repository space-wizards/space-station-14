using System;
using System.Diagnostics.CodeAnalysis;
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.GameObjects.Components;
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
    [UsedImplicitly]
    public class StackSystem : SharedStackSystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StackComponent, InteractUsingMessage>(OnStackInteractUsing);
            SubscribeLocalEvent<StackComponent, StackUseEvent>(OnStackUse);
            SubscribeLocalEvent<StackComponent, StackSplitEvent>(OnStackSplit);
            SubscribeLocalEvent<StackTypeSpawnEvent>(OnStackTypeSpawn);
        }

        /// <summary>
        ///     Tries to spawn a stack of a certain type.
        /// </summary>
        private void OnStackTypeSpawn(StackTypeSpawnEvent args)
        {
            if (args.StackType == null)
                return;

            args.Result = EntityManager.SpawnEntity(args.StackType.Spawn, args.SpawnPosition);
            var stack = args.Result.GetComponent<StackComponent>();

            stack.Count = Math.Min(args.Amount, stack.MaxCount);

        }

        /// <summary>
        ///     Try to use an amount of items on this stack.
        /// </summary>
        private void OnStackUse(EntityUid uid, StackComponent stack, StackUseEvent args)
        {
            if (stack.Count < args.Amount)
            {
                args.Result = false;
            }
            else
            {
                stack.Count -= args.Amount;
                args.Result = true;
            }
        }

        /// <summary>
        ///     Try to split this stack into two.
        /// </summary>
        private void OnStackSplit(EntityUid uid, StackComponent stack, StackSplitEvent args)
        {
            if (stack.Count < args.Amount)
                return;

            stack.Count -= args.Amount;

            var prototype = _prototypeManager.TryIndex<StackPrototype>(stack.StackTypeId, out var stackType)
                ? stackType.Spawn
                : stack.Owner.Prototype?.ID ?? null;

            args.Result = EntityManager.SpawnEntity(prototype, args.SpawnPosition);

            if (args.Result.TryGetComponent(out StackComponent? stackComp))
            {
                stackComp.Count = args.Amount;
            }
        }

        private void OnStackInteractUsing(EntityUid uid, StackComponent stack, InteractUsingMessage args)
        {
            if (!args.ItemInHand.TryGetComponent<StackComponent>(out var otherStack))
                return;

            if (!otherStack.StackTypeId.Equals(stack.StackTypeId))
                return;

            var toTransfer = Math.Min(stack.Count, otherStack.AvailableSpace);
            stack.Count -= toTransfer;
            otherStack.Count += toTransfer;

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
                        args.ItemInHand.SpawnTimer(
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

    /// <summary>
    ///     Uses an amount of things from a stack.
    ///     Whether this succeeded is stored in <see cref="Result"/>.
    /// </summary>
    public class StackUseEvent : EntityEventArgs
    {
        public int Amount { get; init; }

        public bool Result { get; set; } = false;
    }

    /// <summary>
    ///     Tries to split a stack into two.
    ///     If this succeeds, <see cref="Result"/> will be the new stack.
    /// </summary>
    public class StackSplitEvent : EntityEventArgs
    {
        public int Amount { get; init; }
        public EntityCoordinates SpawnPosition { get; init; }

        public IEntity? Result { get; set; } = null;
    }

    /// <summary>
    ///     Tries to spawn a stack of a certain type.
    ///     If this succeeds, <see cref="Result"/> will be the new stack.
    /// </summary>
    public class StackTypeSpawnEvent : EntityEventArgs
    {
        public int Amount { get; init; }
        public StackPrototype? StackType { get; init; }
        public EntityCoordinates SpawnPosition { get; init; }

        public IEntity? Result { get; set; } = null;
    }
}
