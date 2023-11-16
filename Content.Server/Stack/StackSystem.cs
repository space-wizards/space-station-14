using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Stack
{
    /// <summary>
    ///     Entity system that handles everything relating to stacks.
    ///     This is a good example for learning how to code in an ECS manner.
    /// </summary>
    [UsedImplicitly]
    public sealed class StackSystem : SharedStackSystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public static readonly int[] DefaultSplitAmounts = { 1, 5, 10, 20, 30, 50 };

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StackComponent, GetVerbsEvent<AlternativeVerb>>(OnStackAlternativeInteract);
        }

        public override void SetCount(EntityUid uid, int amount, StackComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;

            base.SetCount(uid, amount, component);

            // Queue delete stack if count reaches zero.
            if (component.Count <= 0 && !component.Lingering)
                QueueDel(uid);
        }

        /// <summary>
        ///     Try to split this stack into two. Returns a non-null <see cref="Robust.Shared.GameObjects.EntityUid"/> if successful.
        /// </summary>
        public EntityUid? Split(EntityUid uid, int amount, EntityCoordinates spawnPosition, StackComponent? stack = null)
        {
            if (!Resolve(uid, ref stack))
                return null;

            // Try to remove the amount of things we want to split from the original stack...
            if (!Use(uid, amount, stack))
                return null;

            // Get a prototype ID to spawn the new entity. Null is also valid, although it should rarely be picked...
            var prototype = _prototypeManager.TryIndex<StackPrototype>(stack.StackTypeId, out var stackType)
                ? stackType.Spawn
                : Prototype(uid)?.ID;

            // Set the output parameter in the event instance to the newly split stack.
            var entity = Spawn(prototype, spawnPosition);

            if (TryComp(entity, out StackComponent? stackComp))
            {
                // Set the split stack's count.
                SetCount(entity, amount, stackComp);
                // Don't let people dupe unlimited stacks
                stackComp.Unlimited = false;
            }

            var ev = new StackSplitEvent(entity);
            RaiseLocalEvent(uid, ref ev);

            return entity;
        }

        /// <summary>
        ///     Spawns a stack of a certain stack type. See <see cref="StackPrototype"/>.
        /// </summary>
        public EntityUid Spawn(int amount, StackPrototype prototype, EntityCoordinates spawnPosition)
        {
            // Set the output result parameter to the new stack entity...
            var entity = Spawn(prototype.Spawn, spawnPosition);
            var stack = Comp<StackComponent>(entity);

            // And finally, set the correct amount!
            SetCount(entity, amount, stack);
            return entity;
        }

        /// <summary>
        ///     Say you want to spawn 97 units of something that has a max stack count of 30.
        ///     This would spawn 3 stacks of 30 and 1 stack of 7.
        /// </summary>
        public List<EntityUid> SpawnMultiple(string entityPrototype, int amount, EntityCoordinates spawnPosition)
        {
            var proto = _prototypeManager.Index<EntityPrototype>(entityPrototype);
            proto.TryGetComponent<StackComponent>(out var stack);
            var maxCountPerStack = GetMaxCount(stack);
            var spawnedEnts = new List<EntityUid>();
            while (amount > 0)
            {
                var entity = Spawn(entityPrototype, spawnPosition);
                spawnedEnts.Add(entity);
                var countAmount = Math.Min(maxCountPerStack, amount);
                SetCount(entity, countAmount);
                amount -= countAmount;
            }
            return spawnedEnts;
        }

        private void OnStackAlternativeInteract(EntityUid uid, StackComponent stack, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || args.Hands == null || stack.Count == 1)
                return;

            AlternativeVerb halve = new()
            {
                Text = Loc.GetString("comp-stack-split-halve"),
                Category = VerbCategory.Split,
                Act = () => UserSplit(uid, args.User, stack.Count / 2, stack),
                Priority = 1
            };
            args.Verbs.Add(halve);

            var priority = 0;
            foreach (var amount in DefaultSplitAmounts)
            {
                if (amount >= stack.Count)
                    continue;

                AlternativeVerb verb = new()
                {
                    Text = amount.ToString(),
                    Category = VerbCategory.Split,
                    Act = () => UserSplit(uid, args.User, amount, stack),
                    // we want to sort by size, not alphabetically by the verb text.
                    Priority = priority
                };

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
                Popup.PopupCursor(Loc.GetString("comp-stack-split-too-small"), userUid, PopupType.Medium);
                return;
            }

            if (Split(uid, amount, userTransform.Coordinates, stack) is not {} split)
                return;

            Hands.PickupOrDrop(userUid, split);

            Popup.PopupCursor(Loc.GetString("comp-stack-split"), userUid);
        }
    }
}
