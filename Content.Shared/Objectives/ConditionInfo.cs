using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Objectives
{
    [Serializable, NetSerializable]
    public sealed class ConditionInfo
    {
        public string Title { get; }
        public string Description { get; }
        public SpriteSpecifier SpriteSpecifier { get; }
        public float Progress { get; }

        public ConditionInfo(string title, string description, SpriteSpecifier spriteSpecifier, float progress)
        {
            Title = title;
            Description = description;
            SpriteSpecifier = spriteSpecifier;
            Progress = progress;
        }
    }
}
