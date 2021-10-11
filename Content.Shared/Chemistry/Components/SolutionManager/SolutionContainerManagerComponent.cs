using System.Collections.Generic;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Chemistry.Components.SolutionManager
{
    [NetworkedComponent()]
    [RegisterComponent]
    [DataDefinition]
    [Friend(typeof(SolutionContainerSystem))]
    public class SolutionContainerManagerComponent : Component
    {
        public override string Name => "SolutionContainerManager";

        [ViewVariables]
        [DataField("solutions")]
        public readonly Dictionary<string, Solution> Solutions = new();
    }
}
