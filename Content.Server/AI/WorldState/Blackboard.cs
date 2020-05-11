using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

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
        
        // Eventually a Group blackboard will be added where AI can share knowledge with each other
        
        // Cache the known types
        private static readonly Lazy<List<Type>> _aiStates = new Lazy<List<Type>>(GetStates);

        private readonly Dictionary<Type, IAiState> _states = new Dictionary<Type, IAiState>();
        private readonly List<ICachedState> _cachedStates = new List<ICachedState>();
        private readonly List<IPlanningState> _planningStates = new List<IPlanningState>();

        public Blackboard(IEntity owner)
        {
            Setup(owner);
        }

        private static List<Type> GetStates()
        {
            var aiStates = new List<Type>();
            var reflectionManager = IoCManager.Resolve<IReflectionManager>();

            foreach (var state in reflectionManager.GetAllChildren(typeof(IAiState)))
            {
                aiStates.Add(state);
            }

            return aiStates;
        }

        private void Setup(IEntity owner)
        {
            DebugTools.AssertNotNull(_aiStates);
            var typeFactory = IoCManager.Resolve<IDynamicTypeFactory>();

            foreach (var state in _aiStates.Value)
            {
                var newState = (IAiState) typeFactory.CreateInstance(state);
                newState.Setup(owner);
                _states.Add(newState.GetType(), newState);

                if (newState is ICachedState cachedState)
                {
                    _cachedStates.Add(cachedState);
                }

                if (newState is IPlanningState planningState)
                {
                    _planningStates.Add(planningState);
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
