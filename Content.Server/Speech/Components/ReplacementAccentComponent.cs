using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Speech.Components
{
    [Prototype("accent")]
    public sealed class ReplacementAccentPrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("words")]
        public string[] Words = default!;
    }

    /// <summary>
    /// Replaces any spoken sentences with a random word.
    /// </summary>
    [RegisterComponent]
    public class ReplacementAccentComponent : Component
    {
        public override string Name => "ReplacementAccent";

        [DataField("accent", required: true)]
        public ReplacementAccentPrototype Accent = default!;
    }
}
