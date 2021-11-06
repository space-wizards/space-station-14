using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Nuke
{
    [Serializable, NetSerializable]
    public sealed class NukeEjectMessage : BoundUserInterfaceMessage
    {
    }
}
