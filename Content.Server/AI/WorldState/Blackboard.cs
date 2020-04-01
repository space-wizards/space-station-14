using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Server.AI.WorldState
{
    public sealed class Blackboard
    {
        // Cache the known types
        private static List<Type> _aiStates;

        private readonly Dictionary<Type, IAiState> _states = new Dictionary<Type, IAiState>();
        private readonly List<ICachedState> _cachedStates = new List<ICachedState>();
        private readonly List<IPlanningState> _planningStates = new List<IPlanningState>();

        public Blackboard(IEntity owner)
        {
            Setup(owner);
        }

        private void GetStates()
        {
            _aiStates = new List<Type>();
            var reflectionManager = IoCManager.Resolve<IReflectionManager>();

            foreach (var state in reflectionManager.GetAllChildren(typeof(IAiState)))
            {
                _aiStates.Add(state);
            }
        }

        private void Setup(IEntity owner)
        {
            if (_aiStates == null)
            {
                GetStates();
            }

            DebugTools.AssertNotNull(_aiStates);
            var typeFactory = IoCManager.Resolve<IDynamicTypeFactory>();

            foreach (var state in _aiStates)
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
