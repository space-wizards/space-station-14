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

        #region SpawnAtPosition

        /// <summary>
        /// Spawns a stack of a certain stack type and sets its count. Won't set the stack over its max.
        /// </summary>
        /// <param name="count">The amount to set the spawned stack to.</param>
        [PublicAPI]
        public EntityUid SpawnAtPosition(int count, StackPrototype prototype, EntityCoordinates spawnPosition)
        {
            var entity = SpawnAtPosition(prototype.Spawn, spawnPosition); // The real SpawnAtPosition

            SetCount((entity, null), count);
            return entity;
        }

        /// <inheritdoc cref="SpawnAtPosition(int, StackPrototype, EntityCoordinates)"/>
        [PublicAPI]
        public EntityUid SpawnAtPosition(int count, ProtoId<StackPrototype> id, EntityCoordinates spawnPosition)
        {
            var proto = _prototypeManager.Index(id);
            return SpawnAtPosition(count, proto, spawnPosition);
        }

        /// <summary>
        /// Say you want to spawn 97 units of something that has a max stack count of 30.
        /// This would spawn 3 stacks of 30 and 1 stack of 7.
        /// </summary>
        /// <returns>The entities spawned.</returns>
        /// <remarks> If the entity to spawn doesn't have stack component this will spawn a bunch of single items. </remarks>
        private List<EntityUid> SpawnMultipleAtPosition(EntProtoId entityPrototype,
                                                        List<int> amounts,
                                                        EntityCoordinates spawnPosition)
        {
            if (amounts.Count <= 0)
            {
                Log.Error(
                    $"Attempted to spawn stacks of nothing: {entityPrototype}, {amounts}. Trace: {Environment.StackTrace}");
                return new();
            }

            var spawnedEnts = new List<EntityUid>();
            foreach (var count in amounts)
            {
                var entity = SpawnAtPosition(entityPrototype, spawnPosition); // The real SpawnAtPosition
                spawnedEnts.Add(entity);
                if (TryComp<StackComponent>(entity, out var stackComp)) // prevent errors from the Resolve
                    SetCount((entity, stackComp), count);
            }

            return spawnedEnts;
        }

        /// <inheritdoc cref="SpawnMultipleAtPosition(EntProtoId, List{int}, EntityCoordinates)"/>
        [PublicAPI]
        public List<EntityUid> SpawnMultipleAtPosition(EntProtoId entityPrototypeId,
                                                       int amount,
                                                       EntityCoordinates spawnPosition)
        {
            return SpawnMultipleAtPosition(entityPrototypeId,
                                            CalculateSpawns(entityPrototypeId, amount),
                                            spawnPosition);
        }

        /// <inheritdoc cref="SpawnMultipleAtPosition(EntProtoId, List{int}, EntityCoordinates)"/>
        [PublicAPI]
        public List<EntityUid> SpawnMultipleAtPosition(EntityPrototype entityProto,
                                                       int amount,
                                                       EntityCoordinates spawnPosition)
        {
            return SpawnMultipleAtPosition(entityProto.ID,
                                            CalculateSpawns(entityProto, amount),
                                            spawnPosition);
        }

        /// <inheritdoc cref="SpawnMultipleAtPosition(EntProtoId, List{int}, EntityCoordinates)"/>
        [PublicAPI]
        public List<EntityUid> SpawnMultipleAtPosition(StackPrototype stack,
                                                       int amount,
                                                       EntityCoordinates spawnPosition)
        {
            return SpawnMultipleAtPosition(stack.Spawn,
                                            CalculateSpawns(stack, amount),
                                            spawnPosition);
        }

        /// <inheritdoc cref="SpawnMultipleAtPosition(EntProtoId, List{int}, EntityCoordinates)"/>
        [PublicAPI]
        public List<EntityUid> SpawnMultipleAtPosition(ProtoId<StackPrototype> stackId,
                                                       int amount,
                                                       EntityCoordinates spawnPosition)
        {
            var stackProto = _prototypeManager.Index(stackId);
            return SpawnMultipleAtPosition(stackProto.Spawn,
                                            CalculateSpawns(stackProto, amount),
                                            spawnPosition);
        }

        #endregion
        #region SpawnNextToOrDrop

        /// <inheritdoc cref="SpawnAtPosition(int, StackPrototype, EntityCoordinates)"/>
        [PublicAPI]
        public EntityUid SpawnNextToOrDrop(int amount, StackPrototype prototype, EntityUid source)
        {
            var entity = SpawnNextToOrDrop(prototype.Spawn, source); // The real SpawnNextToOrDrop
            SetCount((entity, null), amount);
            return entity;
        }

        /// <inheritdoc cref="SpawnNextToOrDrop(int, StackPrototype, EntityUid)"/>
        [PublicAPI]
        public EntityUid SpawnNextToOrDrop(int amount, ProtoId<StackPrototype> id, EntityUid source)
        {
            var proto = _prototypeManager.Index(id);
            return SpawnNextToOrDrop(amount, proto, source);
        }

        /// <inheritdoc cref="SpawnMultipleAtPosition(EntProtoId, List{int}, EntityCoordinates)"/>
        private List<EntityUid> SpawnMultipleNextToOrDrop(EntProtoId entityPrototype,
                                                          List<int> amounts,
                                                          EntityUid target)
        {
            if (amounts.Count <= 0)
            {
                Log.Error(
                    $"Attempted to spawn stacks of nothing: {entityPrototype}, {amounts}. Trace: {Environment.StackTrace}");
                return new();
            }

            var spawnedEnts = new List<EntityUid>();
            foreach (var count in amounts)
            {
                var entity = SpawnNextToOrDrop(entityPrototype, target); // The real SpawnNextToOrDrop
                spawnedEnts.Add(entity);
                if (TryComp<StackComponent>(entity, out var stackComp)) // prevent errors from the Resolve
                    SetCount((entity, stackComp), count);
            }

            return spawnedEnts;
        }

        /// <inheritdoc cref="SpawnMultipleNextToOrDrop(EntProtoId, List{int}, EntityUid)"/>
        [PublicAPI]
        public List<EntityUid> SpawnMultipleNextToOrDrop(EntProtoId stack,
                                                         int amount,
                                                         EntityUid target)
        {
            return SpawnMultipleNextToOrDrop(stack,
                                             CalculateSpawns(stack, amount),
                                             target);
        }

        /// <inheritdoc cref="SpawnMultipleNextToOrDrop(EntProtoId, List{int}, EntityUid)"/>
        [PublicAPI]
        public List<EntityUid> SpawnMultipleNextToOrDrop(EntityPrototype stack,
                                                         int amount,
                                                         EntityUid target)
        {
            return SpawnMultipleNextToOrDrop(stack.ID,
                                             CalculateSpawns(stack, amount),
                                             target);
        }

        /// <inheritdoc cref="SpawnMultipleNextToOrDrop(EntProtoId, List{int}, EntityUid)"/>
        [PublicAPI]
        public List<EntityUid> SpawnMultipleNextToOrDrop(StackPrototype stack,
                                                         int amount,
                                                         EntityUid target)
        {
            return SpawnMultipleNextToOrDrop(stack.Spawn,
                                             CalculateSpawns(stack, amount),
                                             target);
        }

        /// <inheritdoc cref="SpawnMultipleNextToOrDrop(EntProtoId, List{int}, EntityUid)"/>
        [PublicAPI]
        public List<EntityUid> SpawnMultipleNextToOrDrop(ProtoId<StackPrototype> stackId,
                                                         int amount,
                                                         EntityUid target)
        {
            var stackProto = _prototypeManager.Index(stackId);
            return SpawnMultipleNextToOrDrop(stackProto.Spawn,
                                             CalculateSpawns(stackProto, amount),
                                             target);
        }

        #endregion
        #region Calculate

        /// <summary>
        /// Calculates how many stacks to spawn that total up to <paramref name="amount"/>.
        /// </summary>
        /// <returns>The list of stack counts per entity.</returns>
        private List<int> CalculateSpawns(int maxCountPerStack, int amount)
        {
            var amounts = new List<int>();
            while (amount > 0)
            {
                var countAmount = Math.Min(maxCountPerStack, amount);
                amount -= countAmount;
                amounts.Add(countAmount);
            }

            return amounts;
        }

        /// <inheritdoc cref="CalculateSpawns(int, int)"/>
        private List<int> CalculateSpawns(StackPrototype stackProto, int amount)
        {
            return CalculateSpawns(GetMaxCount(stackProto), amount);
        }

        /// <inheritdoc cref="CalculateSpawns(int, int)"/>
        private List<int> CalculateSpawns(EntityPrototype entityPrototype, int amount)
        {
            return CalculateSpawns(GetMaxCount(entityPrototype), amount);
        }

        /// <inheritdoc cref="CalculateSpawns(int, int)"/>
        private List<int> CalculateSpawns(EntProtoId entityId, int amount)
        {
            return CalculateSpawns(GetMaxCount(entityId), amount);
        }

        #endregion
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
