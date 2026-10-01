using Content.Shared.Hands.EntitySystems;
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
    public sealed partial class StackSystem : SharedStackSystem
    {
        [Dependency] private SharedHandsSystem _hands = default!;
        [Dependency] private SharedPopupSystem _popup = default!;
        [Dependency] private SharedTransformSystem _transform = default!;

        [Dependency] private EntityQuery<StackComponent> _stackQuery;

        #region Spawning

        /// <inheritdoc />
        [PublicAPI]
        public override EntityUid? Split(Entity<StackComponent?> ent, int amount, EntityCoordinates spawnPosition, EntityUid? user = null)
        {
            if (!_stackQuery.Resolve(ent.Owner, ref ent.Comp))
                return null;

            // Try to remove the amount of things we want to split from the original stack...
            if (!TryUse(ent, amount))
                return null;

            if (!ProtoMan.Resolve(ent.Comp.StackTypeId, out var stackType))
                return null;

            // Set the output parameter in the event instance to the newly split stack.
            var newEntity = SpawnAtPosition(stackType.Spawn, spawnPosition);

            // There should always be a StackComponent
            var stackComp = _stackQuery.Comp(newEntity);

            SetCount((newEntity, stackComp), amount);
            stackComp.Unlimited = false; // Don't let people dupe unlimited stacks
            Dirty(newEntity, stackComp);

            var ev = new StackSplitEvent(newEntity, user);
            RaiseLocalEvent(ent, ref ev);

            return newEntity;
        }

        #endregion
    }
}
