using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Speech.Components
{
    [Prototype("accent")]
    public readonly record struct ReplacementAccentPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [DataField("words")] public readonly string[] Words = default!;
    }

    /// <summary>
    /// Replaces any spoken sentences with a random word.
    /// </summary>
    [RegisterComponent]
    public sealed class ReplacementAccentComponent : Component
    {
        [DataField("accent", customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>), required: true)]
        public string Accent = default!;
    }
}
