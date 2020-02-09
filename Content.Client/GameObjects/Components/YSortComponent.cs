using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Client.GameObjects.Components
{
    [RegisterComponent]
    public class YSortComponent : Component
    {
        private bool _enabled = true;
        private float _offset = -0.5f;
        public override string Name => "YSort";
        public GridCoordinates OldPosition { get; set; } = default;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public float Offset
        {
            get => _offset;
            set => _offset = value;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _enabled, "enabled", true);
            serializer.DataField(ref _offset, "offset", -0.5f);
        }
    }
}
