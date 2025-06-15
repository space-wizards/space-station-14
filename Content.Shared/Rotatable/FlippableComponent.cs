using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Rotatable
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class FlippableComponent : Component
    {
        /// <summary>
        ///     Entity to replace this entity with when the current one is 'flipped'.
        /// </summary>
        [DataField(required: true)]
        public EntProtoId MirrorEntity = default!;
    }
}
