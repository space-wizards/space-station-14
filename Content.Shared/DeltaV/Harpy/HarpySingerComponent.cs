using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization;

namespace Content.Shared.DeltaV.Harpy
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class HarpySingerComponent : Component
    {
        [DataField("midiActionId", serverOnly: true,
            customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? MidiActionId = "ActionHarpyPlayMidi";

        [DataField("midiAction", serverOnly: true)] // server only, as it uses a server-BUI event !type
        public EntityUid? MidiAction;
    }
}
