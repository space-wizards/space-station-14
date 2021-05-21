using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Appearance
{
    /// <summary>
    ///     Contains data for a single hair style
    /// </summary>
    [Prototype("spriteAccessory")]
    public sealed class SpriteAccessoryPrototype : IPrototype, ISerializationHooks
    {
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("categories", required: true)]
        public SpriteAccessoryCategories Categories { get; } = default!;

        public string Name { get; private set; } = default!;

        [DataField("sprite", required: true)]
        public SpriteSpecifier Sprite { get; } = default!;

        [DataField("priority")] public int Priority { get; } = 0;

        void ISerializationHooks.AfterDeserialization()
        {
            Name = Loc.GetString($"accessory-{ID}");
        }
    }
}
