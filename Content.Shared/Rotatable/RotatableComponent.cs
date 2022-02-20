using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using Robust.Shared.Maths;

namespace Content.Shared.Rotatable
{
    [RegisterComponent]
    public sealed class RotatableComponent : Component
    {
        /// <summary>
        ///     If true, this entity can be rotated even while anchored.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("rotateWhileAnchored")]
        public bool RotateWhileAnchored { get; protected set; }

        /// <summary>
        ///     If true, will rotate entity in players direction when pulled
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("rotateWhilePulling")]
        public bool RotateWhilePulling { get; protected set; } = true;

        /// <summary>
        ///     The angular value to change when using the rotate verbs.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("increment")]
        public Angle Increment { get; protected set; } = Angle.FromDegrees(90);
    }
}
