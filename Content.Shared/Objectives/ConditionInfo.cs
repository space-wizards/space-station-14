using System;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Objectives
{
    [Serializable, NetSerializable]
    public class ConditionInfo
    {
        public string Title { get; }
        public string Description { get; }
        public SpriteSpecifier SpriteSpecifier { get; }

        public ConditionInfo(string title, string description, SpriteSpecifier spriteSpecifier)
        {
            Title = title;
            Description = description;
            SpriteSpecifier = spriteSpecifier;
        }
    }
}
