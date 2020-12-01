using System;

namespace Content.Server.GameObjects
{
    [Flags]
    public enum VisibilityFlags : byte
    {
        Normal = 1,
        Ghost = 2,
    }
}
