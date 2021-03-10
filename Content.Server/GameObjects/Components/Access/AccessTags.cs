using System;

namespace Content.Server.GameObjects.Components.Access
{
    [Flags]
    public enum AccessTags
    {
        None = 0,
        Command = 1,
        Security = 2,
        Medical = 4,
        Engineering = 8,
        Research = 16,
        Cargo = 32,
        Maintenance = 64,
        Service = 128,
        External = 256

    }
}
