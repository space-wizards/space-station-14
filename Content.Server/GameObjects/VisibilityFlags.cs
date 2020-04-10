using System;

namespace Content.Server.GameObjects
{
    [Flags]
    public enum VisibilityFlags
    {
        None = 0,
        Normal = 1,
        Ghost = 2,
    }
}
