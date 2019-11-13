using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Research
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ResearchPointSourceComponent : ResearchClientComponent
    {
        public override string Name => "ResearchPointSource";

        private int _pointsPerSecond;
        private bool _active;

        [ViewVariables]
        public int PointsPerSecond
        {
            get => _pointsPerSecond;
            set => value = _pointsPerSecond;
        }

        [ViewVariables]
        public bool Active
        {
            get => _active;
            set => value = _active;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _pointsPerSecond, "pointspersecond", 0);
            serializer.DataField(ref _active, "active", false);
        }
    }
}
