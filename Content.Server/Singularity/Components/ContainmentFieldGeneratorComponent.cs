using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Physics;
using Content.Shared.Singularity.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;

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

    }
}
