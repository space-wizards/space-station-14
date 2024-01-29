using Content.Shared.DoAfter;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Toilet
{
    [RegisterComponent]
    public sealed partial class ToiletComponent : Component
    {
        [DataField("pryLidTime")]
        public float PryLidTime = 1f;

        [DataField("pryingQuality", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        public string PryingQuality = "Prying";

        [DataField("toggleSound")]
        public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/Effects/toilet_seat_down.ogg");

        [DataField("lidOpen")]
        public bool LidOpen = false;

        [DataField("isSeatUp")]
        public bool IsSeatUp = false;
    }

    [Serializable, NetSerializable]
    public sealed partial class ToiletPryDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
