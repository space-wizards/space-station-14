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

        [ViewVariables]
        [DataField("reflectorType")]
        public ReflectorType Type { get; set; } = ReflectorType.SingleSide;
    }

    public enum ReflectorType
    {
        SingleSide,
        DoubleSide,
        Box
    }
}
