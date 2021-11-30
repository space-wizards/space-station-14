using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
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
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public static readonly int[] DefaultSplitAmounts = { 1, 5, 10, 20, 30, 50 };

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StackComponent, InteractUsingEvent>(OnStackInteractUsing);
            SubscribeLocalEvent<StackComponent, GetAlternativeVerbsEvent>(OnStackAlternativeInteract);
        }

        /// <summary>
        ///     Try to split this stack into two. Returns a non-null <see cref="IEntity"/> if successful.
        /// </summary>
        public EntityUid? Split(EntityUid uid, int amount, EntityCoordinates spawnPosition, SharedStackComponent? stack = null)
        {
            if (!Resolve(uid, ref stack))
                return null;

            // Get a prototype ID to spawn the new entity. Null is also valid, although it should rarely be picked...
            var prototype = _prototypeManager.TryIndex<StackPrototype>(stack.StackTypeId, out var stackType)
                ? stackType.Spawn
                : stack.Owner.Prototype?.ID;

            // Try to remove the amount of things we want to split from the original stack...
            if (!Use(uid, amount, stack))
                return null;

            // Set the output parameter in the event instance to the newly split stack.
            var entity = EntityManager.SpawnEntity(prototype, spawnPosition).Uid;

            if (EntityManager.TryGetComponent(entity, out SharedStackComponent? stackComp))
            {
                // Set the split stack's count.
                SetCount(entity, amount, stackComp);
                // Don't let people dupe unlimited stacks
                stackComp.Unlimited = false;
            }

            return entity;
        }

        /// <summary>
        ///     Spawns a stack of a certain stack type. See <see cref="StackPrototype"/>.
        /// </summary>
        public EntityUid Spawn(int amount, StackPrototype prototype, EntityCoordinates spawnPosition)
        {
            // Set the output result parameter to the new stack entity...
            var entity = EntityManager.SpawnEntity(prototype.Spawn, spawnPosition).Uid;
            var stack = EntityManager.GetComponent<StackComponent>(entity);

            // And finally, set the correct amount!
            SetCount(entity, amount, stack);
            return entity;
        }

        private void OnStackInteractUsing(EntityUid uid, StackComponent stack, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!args.Used.TryGetComponent<StackComponent>(out var otherStack))
                return;

            if (!otherStack.StackTypeId.Equals(stack.StackTypeId))
                return;

            var toTransfer = Math.Min(stack.Count, otherStack.AvailableSpace);
            SetCount(uid, stack.Count - toTransfer, stack);
            SetCount(args.Used.Uid, otherStack.Count + toTransfer, otherStack);

            var popupPos = args.ClickLocation;

            if (!popupPos.IsValid(EntityManager))
            {
                popupPos = args.User.Transform.Coordinates;
            }

            var filter = Filter.Entities(args.User.Uid);

            switch (toTransfer)
            {
                case > 0:
                    _popupSystem.PopupCoordinates($"+{toTransfer}", popupPos, filter);

                    if (otherStack.AvailableSpace == 0)
                    {
                        _popupSystem.PopupCoordinates(Loc.GetString("comp-stack-becomes-full"),
                            popupPos.Offset(new Vector2(0, -0.5f)) , filter);
                    }

                    break;

                case 0 when otherStack.AvailableSpace == 0:
                    _popupSystem.PopupCoordinates(Loc.GetString("comp-stack-already-full"), popupPos, filter);
                    break;
            }

            args.Handled = true;
        }

        private void OnStackAlternativeInteract(EntityUid uid, StackComponent stack, GetAlternativeVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            Verb halve = new();
            halve.Text = Loc.GetString("comp-stack-split-halve");
            halve.Category = VerbCategory.Split;
            halve.Act = () => UserSplit(uid, args.User.Uid, stack.Count / 2, stack);
            halve.Priority = 1;
            args.Verbs.Add(halve);

            var priority = 0;
            foreach (var amount in DefaultSplitAmounts)
            {
                if (amount >= stack.Count)
                    continue;

                Verb verb = new();
                verb.Text = amount.ToString();
                verb.Category = VerbCategory.Split;
                verb.Act = () => UserSplit(uid, args.User.Uid, amount, stack);

                // we want to sort by size, not alphabetically by the verb text.
                verb.Priority = priority;
                priority--;

                args.Verbs.Add(verb);
            }
        }

        private void UserSplit(EntityUid uid, EntityUid userUid, int amount,
            StackComponent? stack = null,
            TransformComponent? userTransform = null)
        {
            if (!Resolve(uid, ref stack))
                return;

            if (!Resolve(userUid, ref userTransform))
                return;

            if (amount <= 0)
            {
                _popupSystem.PopupCursor(Loc.GetString("comp-stack-split-too-small"), Filter.Entities(userUid));
                return;
            }

            if (EntityManager.TryGetComponent<HandsComponent>(userUid, out var hands))
            {
                if (hands.TryGetActiveHeldEntity(out var heldItem) && heldItem != stack.Owner)
                {
                    return;
                }
            }
            else
            {
                return;
            }

            if(Split(uid, amount, userTransform.Coordinates, stack) is not {} splitStack)
                return;

            if (EntityManager.TryGetComponent<ItemComponent>(splitStack, out var item))
            {
                hands.PutInHandOrDrop(item);
            }

            _popupSystem.PopupCursor(Loc.GetString("comp-stack-split"), Filter.Entities(userUid));
        }
    }
}
