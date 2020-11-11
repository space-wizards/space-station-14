#nullable enable
using Robust.Shared.Utility;

namespace Content.Shared.Objectives
{
    public sealed class PrototypeIcon : SpriteSpecifier
    {
        public readonly string PrototypeId;

        public PrototypeIcon(string prototypeId)
        {
            PrototypeId = prototypeId;
        }

        public override bool Equals(object? obj)
        {
            if (obj is PrototypeIcon prototypeIcon)
                return PrototypeId == prototypeIcon.PrototypeId;
            return false;
        }

        public override int GetHashCode()
        {
            return PrototypeId.GetHashCode();
        }
    }
}
