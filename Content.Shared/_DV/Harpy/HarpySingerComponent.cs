using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._DV.Harpy
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class HarpySingerComponent : Component
    {
        [DataField(serverOnly: true)]
        public EntProtoId MidiActionId = "ActionHarpyPlayMidi";

        [DataField(serverOnly: true)] // server only, as it uses a server-BUI event !type
        public EntityUid? MidiAction;
    }
}
