using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Audio;

namespace Content.Shared.Nutrition.Components
{
    [Access(typeof(SharedCreamPieSystem))]
    [RegisterComponent]
    public sealed partial class CreamPieComponent : Component
    {
        [DataField("paralyzeTime")]
        public float ParalyzeTime { get; private set; } = 1f;

        [DataField("sound")]
        public SoundSpecifier Sound { get; private set; } = new SoundCollectionSpecifier("desecration");

        [ViewVariables]
        public bool Splatted { get; set; } = false;

        public const string PayloadSlotName = "payloadSlot";
    }
}
