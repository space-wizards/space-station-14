using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Rotatable
{
    [RegisterComponent]
    public class RotatableComponent : Component
    {
        public override string Name => "Rotatable";

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
    }
}
