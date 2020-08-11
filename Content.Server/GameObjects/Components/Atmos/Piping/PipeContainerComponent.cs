using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Atmos
{
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
