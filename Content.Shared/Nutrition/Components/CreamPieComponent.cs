using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Sound;

namespace Content.Shared.Nutrition.Components
{
    [Access(typeof(SharedCreamPieSystem))]
    [RegisterComponent]
    public sealed class CreamPieComponent : Component
    {
        [ViewVariables]
        [DataField("paralyzeTime")]
        public float ParalyzeTime { get; } = 1f;

        [ViewVariables]
        [DataField("sound")]
        public SoundSpecifier Sound { get; } = new SoundCollectionSpecifier("desecration");

        [ViewVariables]
        public bool Splatted { get; set; } = false;
    }
}
