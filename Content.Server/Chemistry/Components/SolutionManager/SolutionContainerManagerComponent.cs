using System.Collections.Generic;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Chemistry.Components.SolutionManager
{
    [RegisterComponent]
    [Friend(typeof(SolutionContainerSystem))]
    public class SolutionContainerManagerComponent : Component
    {
        public override string Name => "SolutionContainerManager";

        [ViewVariables]
        [DataField("solutions")]
        public readonly Dictionary<string, Solution> Solutions = new();
    }
}
