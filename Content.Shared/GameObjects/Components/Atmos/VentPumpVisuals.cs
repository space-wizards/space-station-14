using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Atmos
{
    [Serializable, NetSerializable]
    public enum VentPumpVisuals : byte
    {
        State,
    }

    [Serializable, NetSerializable]
    public enum VentPumpState : byte
    {
        Off,
        In,
        Out,
        Welded,
    }
}
