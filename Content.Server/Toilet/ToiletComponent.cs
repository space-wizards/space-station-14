using Content.Server.Act;
using Content.Server.Chat.Managers;
using Content.Shared.Sound;
using Content.Shared.Tools;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Toilet
{
    [RegisterComponent]
    public sealed class ToiletComponent : Component, ISuicideAct
    {
        [DataField("pryLidTime")]
        public float PryLidTime = 1f;

        [DataField("pryingQuality", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        public string PryingQuality = "Prying";

        [DataField("toggleSound")]
        public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/Effects/toilet_seat_down.ogg");

        public bool LidOpen = false;
        public bool IsSeatUp = false;
        public bool IsPrying = false;

        // todo: move me to ECS
        SuicideKind ISuicideAct.Suicide(EntityUid victim, IChatManager chat)
        {
            return EntitySystem.Get<ToiletSystem>().Suicide(Owner, victim, this);
        }

    }
}
