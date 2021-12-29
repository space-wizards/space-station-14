using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using Robust.Shared.Maths;
using Content.Shared.Whitelist;

namespace Content.Server.Reflector
{
    [RegisterComponent, Friend(typeof(ReflectorSystem))]
    public class ReflectorComponent : Component
    {
        public override string Name => "Reflector";

        [DataField("whitelist")]
        public EntityWhitelist Whitelist = new();

        /// <summary>
        ///     The Angle of the reflector
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("Angle")]
        public Angle Angle { get; set; } = Angle.Zero;

        /// <summary>
        ///     If the Reflector is one sided(0), two sided(1), or a box Reflector(2)
        /// </summary>
        [ViewVariables]
        [DataField("Type")]
        public ReflectorType Type { get; set; } = ReflectorType.SingleSide;
    }

    public enum ReflectorType
    {
        SingleSide,
        DualSide,
        Box
    }
}
