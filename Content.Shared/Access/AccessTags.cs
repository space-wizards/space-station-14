using System;

namespace Content.Shared.Access
{
    [Flags]
    public enum AccessTags : ushort
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
        External = 256,

        AllAccess = Command | Security | Medical |
                    Engineering | Research | Cargo |
                    Maintenance | Service | External,

    }


}
