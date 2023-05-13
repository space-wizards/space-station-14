using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Audio;

namespace Content.Shared.Nutrition.Components
{
    [Access(typeof(SharedCreamPieSystem))]
    [RegisterComponent]
    public sealed class CreamPieComponent : Component
    {
        [DataField("paralyzeTime")]
        public float ParalyzeTime { get; } = 1f;

        [DataField("sound")]
        public SoundSpecifier Sound { get; } = new SoundCollectionSpecifier("desecration");

        [ViewVariables]
        public bool Splatted { get; set; } = false;

        public const string PayloadSlotName = "payloadSlot";
    }
}
