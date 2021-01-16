#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Power
{
    [RegisterComponent]
    public class DummyWireComponent : Component
    {
        public override string Name => "DummyWire";

        private List<string> _dummyWireProtos = default!;

        private List<IEntity> _dummyWires = new();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _dummyWireProtos, "prototype", new List<string> { "HVDummyWire" });
        }

        public override void Initialize()
        {
            base.Initialize();
            foreach (var proto in _dummyWireProtos)
            {
                var wire = Owner.EntityManager.SpawnEntity(proto, Owner.Transform.Coordinates);
                _dummyWires.Add(wire);
            }
        }

        public override void OnRemove()
        {
            foreach (var wire in _dummyWires)
            {
                Owner.EntityManager.DeleteEntity(wire);
            }

            base.OnRemove();
        }
    }
}
