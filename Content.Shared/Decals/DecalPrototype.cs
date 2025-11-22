using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.Decals
{
    [Prototype]
    public sealed partial class DecalPrototype : IPrototype, IInheritingPrototype
    {
        [IdDataField] public string ID { get; private set; } = null!;
        [DataField("sprite")] public SpriteSpecifier Sprite { get; private set; } = SpriteSpecifier.Invalid;
        [DataField("tags")] public List<string> Tags = new();
        [DataField("showMenu")] public bool ShowMenu = true;

        /// <summary>
        /// If the decal is rotated compared to our eye should we snap it to south.
        /// </summary>
        [DataField("snapCardinals")] public bool SnapCardinals = false;

        /// <summary>
        /// True if this decal is cleanable by default.
        /// </summary>
        [DataField]
        public bool DefaultCleanable;

        /// <summary>
        /// True if this decal has custom colors applied by default
        /// </summary>
        [DataField]
        public bool DefaultCustomColor;

        /// <summary>
        /// True if this decal snaps to a tile by default
        /// </summary>
        [DataField]
        public bool DefaultSnap = true;

        [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<DecalPrototype>))]
        public string[]? Parents { get; private set; }

        [NeverPushInheritance]
        [AbstractDataField]
        public bool Abstract { get; private set; }

    }
}
