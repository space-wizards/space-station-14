using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Sound;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Nutrition.Components
{
    [Friend(typeof(SharedCreamPieSystem))]
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
