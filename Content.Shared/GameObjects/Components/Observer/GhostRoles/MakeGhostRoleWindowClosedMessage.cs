using System;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Observer.GhostRoles
{
    [Serializable, NetSerializable]
    public class MakeGhostRoleWindowClosedMessage : EuiMessageBase
    {
    }
}
