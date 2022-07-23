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
        public int? EntryNumber { get; set; } = null;
        public int Padding { get; set; } = 0;
        public string Localization { get; set; } = string.Empty;
        public (string, object)[]? Arguments { get; set; } = null;
        public SpriteSpecifier? Icon { get; set; } = null;
    }
}
