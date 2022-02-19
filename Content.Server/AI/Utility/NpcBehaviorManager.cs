using System;
using System.Collections.Generic;
using Content.Server.AI.EntitySystems;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.AiLogic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;

namespace Content.Server.AI.Utility
{
    internal interface INpcBehaviorManager
    {
        void Initialize();

        void AddBehaviorSet(UtilityAi npc, string behaviorSet, bool rebuild = true);

        void RemoveBehaviorSet(UtilityAi npc, string behaviorSet, bool rebuild = true);

        void RebuildActions(UtilityAi npc);
    }

    /// <summary>
    ///     Handles BehaviorSets and adding / removing behaviors to NPCs
    /// </summary>
    internal sealed class NpcBehaviorManager : INpcBehaviorManager
    {
        [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;

        private readonly NpcActionComparer _comparer = new();

        private Dictionary<string, List<Type>> _behaviorSets = new();

        public void Initialize()
        {
            IoCManager.InjectDependencies(this);
            var protoManager = IoCManager.Resolve<IPrototypeManager>();
            var reflectionManager = IoCManager.Resolve<IReflectionManager>();

            foreach (var bSet in protoManager.EnumeratePrototypes<BehaviorSetPrototype>())
            {
                var actions = new List<Type>();

                foreach (var act in bSet.Actions)
                {
                    if (!reflectionManager.TryLooseGetType(act, out var parsedType) ||
                        !typeof(IAiUtility).IsAssignableFrom(parsedType))
                    {
                        Logger.Error($"Unable to parse AI action for {act}");
                    }
                    else
                    {
                        actions.Add(parsedType);
                    }
                }

                _behaviorSets[bSet.ID] = actions;
            }
        }

        /// <summary>
        ///     Adds the BehaviorSet to the NPC.
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="behaviorSet"></param>
        /// <param name="rebuild">Set to false if you want to manually rebuild it after bulk updates.</param>
        public void AddBehaviorSet(UtilityAi npc, string behaviorSet, bool rebuild = true)
        {
            if (!_behaviorSets.ContainsKey(behaviorSet))
            {
                Logger.Error($"Tried to add BehaviorSet {behaviorSet} to {npc} but no such BehaviorSet found!");
                return;
            }

            if (!npc.BehaviorSets.Add(behaviorSet))
            {
                Logger.Error($"Tried to add BehaviorSet {behaviorSet} to {npc} which already has the BehaviorSet!");
                return;
            }

            if (rebuild)
                RebuildActions(npc);

            if (npc.BehaviorSets.Count == 1 && !npc.Awake)
            {
                EntitySystem.Get<NPCSystem>().WakeNPC(npc);
            }
        }

        /// <summary>
        ///     Removes the BehaviorSet from the NPC.
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="behaviorSet"></param>
        /// <param name="rebuild">Set to false if yo uwant to manually rebuild it after bulk updates.</param>
        public void RemoveBehaviorSet(UtilityAi npc, string behaviorSet, bool rebuild = true)
        {
            if (!_behaviorSets.TryGetValue(behaviorSet, out var actions))
            {
                Logger.Error($"Tried to remove BehaviorSet {behaviorSet} from {npc} but no such BehaviorSet found!");
                return;
            }

            if (!npc.BehaviorSets.Remove(behaviorSet))
            {
                Logger.Error($"Tried to remove BehaviorSet {behaviorSet} from {npc} but it doesn't have that BehaviorSet!");
                return;
            }

            if (rebuild)
                RebuildActions(npc);

            if (npc.BehaviorSets.Count == 0 && npc.Awake)
            {
                EntitySystem.Get<NPCSystem>().SleepNPC(npc);
            }
        }

        /// <summary>
        ///     Clear our actions and re-instantiate them from our BehaviorSets.
        ///     Will ensure each action is unique.
        /// </summary>
        /// <param name="npc"></param>
        public void RebuildActions(UtilityAi npc)
        {
            npc.AvailableActions.Clear();
            foreach (var bSet in npc.BehaviorSets)
            {
                foreach (var action in GetActions(bSet))
                {
                    if (npc.AvailableActions.Contains(action)) continue;
                    // Setup
                    action.Owner = npc.Owner;

                    // Ad to actions.
                    npc.AvailableActions.Add(action);
                }
            }

            SortActions(npc);
        }

        private IEnumerable<IAiUtility> GetActions(string behaviorSet)
        {
            foreach (var action in _behaviorSets[behaviorSet])
            {
                yield return (IAiUtility) _typeFactory.CreateInstance(action);
            }
        }

        /// <summary>
        ///     Whenever the behavior sets are changed we'll re-sort the actions by bonus
        /// </summary>
        private void SortActions(UtilityAi npc)
        {
            npc.AvailableActions.Sort(_comparer);
        }

        private sealed class NpcActionComparer : Comparer<IAiUtility>
        {
            public override int Compare(IAiUtility? x, IAiUtility? y)
            {
                if (x == null || y == null) return 0;
                return y.Bonus.CompareTo(x.Bonus);
            }
        }
    }
}
