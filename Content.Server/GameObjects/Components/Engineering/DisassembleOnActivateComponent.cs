#nullable enable
using System.Threading;
using Content.Shared.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Engineering
    {
        [RegisterComponent]
        public class DisassembleOnActivateComponent : Component
    {
        public override string Name => "DisassembleOnActivate";
        public override uint? NetID => ContentNetIDs.DISASSEMBLE_ON_ACTIVATE;

        [ViewVariables]
        [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? Prototype { get; }

        [ViewVariables]
        [DataField("doAfter")]
        public float DoAfterTime = 0;

        public CancellationTokenSource TokenSource { get; } = new();
    }
}
