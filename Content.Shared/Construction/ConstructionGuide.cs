using System;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    [Serializable, NetSerializable]
    public sealed class ConstructionGuide
    {
        public readonly ConstructionGuideEntry[] Entries;

        public ConstructionGuide(ConstructionGuideEntry[] entries)
        {
            Entries = entries;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ConstructionGuideEntry
    {
        public readonly string Localization;
        public readonly (string, string)[]? Arguments;
        public readonly bool Numbered;
        public readonly SpriteSpecifier? Sprite;

        public ConstructionGuideEntry(string localization, (string, string)[]? arguments, bool numbered, SpriteSpecifier? sprite)
        {
            Localization = localization;
            Arguments = arguments;
            Sprite = sprite;
            Numbered = numbered;
        }
    }
}
