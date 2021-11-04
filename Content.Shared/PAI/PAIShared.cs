using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.PAI
{
    [Serializable, NetSerializable]
    public enum PAIVisuals : byte
    {
        Status
    }

    [Serializable, NetSerializable]
    public enum PAIStatus : byte
    {
        Off,
        Searching,
        On
    }
}

