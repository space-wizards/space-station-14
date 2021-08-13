using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Chemistry.Solution.Components
{
    [NetworkedComponent()]
    [RegisterComponent]
    [DataDefinition]
    public class SolutionContainerManager : Component
    {
        public override string Name => "SolutionContainer";

        [ViewVariables] [DataField("solutions")]
        public readonly Dictionary<string, Solution> Solutions = new();
    }
}
