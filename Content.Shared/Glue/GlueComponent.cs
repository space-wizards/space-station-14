using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Shared.Glue
{
    [RegisterComponent, NetworkedComponent]
    public sealed class GlueComponent : Component
    {
        [DataField("glueReagent", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
        public string GlueReagent { get; } = "CrazyGlueReagent";

        [DataField("glueUseCost")]
        public FixedPoint2 GlueUseCost { get; } = FixedPoint2.New(1f);

        [DataField("glueSolution")]
        public string GlueSolution { get; } = "CrazyGlue";
    }
}
