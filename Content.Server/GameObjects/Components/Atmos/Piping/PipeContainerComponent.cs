using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Atmos
{
    /// <summary>
    ///     Allows an entity to hold gases from an <see cref="IPipeNet"/>, needed to function with
    ///     a <see cref="BaseScrubberComponent"/>, <see cref="BaseVentComponent"/>, and <see cref="BasePumpComponent"/>. 
    /// </summary>
    [RegisterComponent]
    public class PipeContainerComponent : Component
    {
        public override string Name => "PipeContainer";

        [ViewVariables]
        public IReadOnlyList<Pipe> Pipes => _pipes;
        private List<Pipe> _pipes = new List<Pipe>();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _pipes, "pipes", new List<Pipe>());
        }

        public override void Initialize()
        {
            base.Initialize();
            foreach (var pipe in _pipes)
            {
                pipe.Initialize();
            }
        }
    }
}
