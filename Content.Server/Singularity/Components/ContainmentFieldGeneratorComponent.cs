using Content.Shared.Singularity.Components;

namespace Content.Server.Singularity.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedContainmentFieldGeneratorComponent))]
    public sealed class ContainmentFieldGeneratorComponent : SharedContainmentFieldGeneratorComponent
    {
        private int _powerBuffer;

        [ViewVariables]
        public int PowerBuffer
        {
            get => _powerBuffer;
            set => _powerBuffer = Math.Clamp(value, 0, 6);
        }

        public Tuple<Direction, ContainmentFieldConnection>? Connection1;
        public Tuple<Direction, ContainmentFieldConnection>? Connection2;

        [ViewVariables]
        public bool Enabled;

        [ViewVariables]
        public bool IsConnected;

    }
}
