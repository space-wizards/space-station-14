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
        }

        /// <summary>
        ///     Try to use an amount of items on this stack.
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="amount"></param>
        /// <returns>True if there were enough items to remove, false if not in which case nothing was changed.</returns>
        public bool Use(in SharedStackComponent stack, int amount)
        {
            if (stack.Count < amount)
                return false;

            stack.Count -= amount;
            return true;
        }

        /// <summary>
        ///     Attempts to split this stack in two.
        /// </summary>
        /// <param name="stack">the stack to split from</param>
        /// <param name="amount">amount the new stack will have</param>
        /// <param name="spawnPosition">the position the new stack will spawn at</param>
        /// <param name="newEntity">the new stack entity</param>
        /// <returns></returns>
        public bool Split(in SharedStackComponent stack, int amount, EntityCoordinates spawnPosition, [NotNullWhen(true)] out IEntity? newEntity)
        {
            if (stack.Count >= amount)
            {
                stack.Count -= amount;

                string? prototype;

                if (_prototypeManager.TryIndex<StackPrototype>(stack.StackTypeId, out var stackType))
                    prototype = stackType.Spawn;
                else
                    prototype = stack.Owner.Prototype?.ID ?? null;

                newEntity = EntityManager.SpawnEntity(prototype, spawnPosition);

                if (newEntity.TryGetComponent(out StackComponent? stackComp))
                {
                    stackComp.Count = amount;
                }

                return true;
            }

            newEntity = null;
            return false;
        }
        
        public IEntity SpawnStack(StackPrototype stack, int amount, EntityCoordinates coordinates)
        {
            var ent = EntityManager.SpawnEntity(stack.Spawn, coordinates);
            var stackComponent = ent.GetComponent<StackComponent>();

            stackComponent.Count = Math.Min(amount, stackComponent.MaxCount);

            return ent;
        }

        private void OnStackInteractUsing(EntityUid uid, StackComponent ourStack, InteractUsingMessage args)
        {
            if (!args.ItemInHand.TryGetComponent<StackComponent>(out var otherStack))
                return;

            if (!otherStack.StackTypeId.Equals(ourStack.StackTypeId))
                return;

            var toTransfer = Math.Min(ourStack.Count, otherStack.AvailableSpace);
            ourStack.Count -= toTransfer;
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
}
