using System;

namespace Content.Shared
{
    [Flags]
    public enum ComponentLocation
    {
        Server = 1,
        Client = 2,
        Shared = 3,
    }
}
