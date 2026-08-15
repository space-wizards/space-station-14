using Content.Shared.Popups;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Stack
{
    /// <summary>
    /// Entity system that handles everything relating to stacks.
    /// This is a good example for learning how to code in an ECS manner.
    /// </summary>
    [UsedImplicitly]
    public sealed class StackSystem : SharedStackSystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        #region Spawning

        /// <summary>
        /// Spawns a new entity and moves an amount to it from the stack.
        /// Moves nothing if amount is greater than ent's stack count.
        /// </summary>
        /// <param name="amount"> How much to move to the new entity. </param>
        /// <returns>Null if StackComponent doesn't resolve, or amount to move is greater than ent has available.</returns>
        [PublicAPI]
        public EntityUid? Split(Entity<StackComponent?> ent, int amount, EntityCoordinates spawnPosition)
        {
            if (!Resolve(ent.Owner, ref ent.Comp))
                return null;

            // Try to remove the amount of things we want to split from the original stack...
            if (!TryUse(ent, amount))
                return null;

            if (!_prototypeManager.Resolve(ent.Comp.StackTypeId, out var stackType))
                return null;

            // Set the output parameter in the event instance to the newly split stack.
            var newEntity = SpawnAtPosition(stackType.Spawn, spawnPosition);

            // There should always be a StackComponent
            var stackComp = Comp<StackComponent>(newEntity);

            SetCount((newEntity, stackComp), amount);
            stackComp.Unlimited = false; // Don't let people dupe unlimited stacks
            Dirty(newEntity, stackComp);

            var ev = new StackSplitEvent(newEntity);
            RaiseLocalEvent(ent, ref ev);

            return newEntity;
        }


        #endregion
        #region Event Handlers

        /// <inheritdoc />
        protected override void UserSplit(Entity<StackComponent> stack, Entity<TransformComponent?> user, int amount)
        {
            if (!Resolve(user.Owner, ref user.Comp, false))
                return;

            if (amount <= 0)
            {
                Popup.PopupCursor(Loc.GetString("comp-stack-split-too-small"), user.Owner, PopupType.Medium);
                return;
            }

            if (Split(stack.AsNullable(), amount, user.Comp.Coordinates) is not { } split)
                return;

            Hands.PickupOrDrop(user.Owner, split);

            Popup.PopupCursor(Loc.GetString("comp-stack-split"), user.Owner);
        }
        #endregion
    }
}
