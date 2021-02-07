using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Server.AI.WorldState
{
    // This will also handle the global blackboard at some point
    /// <summary>
    /// Manager the AI blackboard states
    /// </summary>
    public sealed class BlackboardManager
    {
        // Cache the known types
        public IReadOnlyCollection<Type> AiStates => _aiStates;
        private readonly List<Type> _aiStates = new();

        public void Initialize()
        {
            var reflectionManager = IoCManager.Resolve<IReflectionManager>();

            foreach (var state in reflectionManager.GetAllChildren(typeof(IAiState)))
            {
                _aiStates.Add(state);
            }

            DebugTools.AssertNotNull(_aiStates);
        }
    }
}
