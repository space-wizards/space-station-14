using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.WorldState
{
    /// <summary>
    /// The blackboard functions as an AI's repository of knowledge in a common format.
    /// </summary>
    public sealed class Blackboard
    {
        // Some stuff like "My Health" is easy to represent as components but abstract stuff like "How much food is nearby"
        // is harder. This also allows data to be cached if it's being hit frequently.

        // This also stops you from re-writing the same boilerplate everywhere of stuff like "Do I have OuterClothing on?"

        private readonly Dictionary<Type, IAiState> _states = new();
        private readonly List<IPlanningState> _planningStates = new();

        public Blackboard(EntityUid owner)
        {
            Setup(owner);
        }

        private void Setup(EntityUid owner)
        {
            var typeFactory = IoCManager.Resolve<IDynamicTypeFactory>();
            var blackboardManager = IoCManager.Resolve<BlackboardManager>();

            foreach (var state in blackboardManager.AiStates)
            {
                var newState = (IAiState) typeFactory.CreateInstance(state);
                newState.Setup(owner);
                _states.Add(newState.GetType(), newState);

                switch (newState)
                {
                    case IPlanningState planningState:
                        _planningStates.Add(planningState);
                        break;
                }
            }
        }

        /// <summary>
        /// All planning states will have their values reset
        /// </summary>
        public void ResetPlanning()
        {
            foreach (var state in _planningStates)
            {
                state.Reset();
            }
        }

        public void GetStoredState<T>(Type type, out StoredStateData<T> state)
        {
            state = (StoredStateData<T>) _states[type];
        }

        /// <summary>
        /// Get the AI state class
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public T GetState<T>() where T : IAiState
        {
            return (T) _states[typeof(T)];
        }
    }
}
